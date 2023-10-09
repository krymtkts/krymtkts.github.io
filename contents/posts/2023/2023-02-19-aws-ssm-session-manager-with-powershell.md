---
title: "AWS SSM Session Manager を PowerShell で"
tags: ["aws","powershell"]
---

AWS SSM Session Manager を使って EC2 Instance とかに接続するやつがある。
AWS CLI であれば Session Mangger plugin[^1] を入れておけば `aws ssm start-session` で SSH 感覚でつなげる。 Security Group に自分とこの外部 IP を含めなくていいし、最高のやつだ。

だが AWS Tools for PowerShell で対になる `Start-SSMSession`は [StartSession API](https://docs.aws.amazon.com/systems-manager/latest/APIReference/API_StartSession.html) の戻り値を返すだけ。
(公式回答なのか？) AWS dev サポの方が AWS Tools for PowerShell の Issue[^2] にて Session Manager plugin に対応してなさそうなコメントしてる。
また Session Manager plugin 自体、その本丸である WebSocket の URL やらをどのようにこねくり回して渡せばいいかが文書化されてない。

PowerShell では Session Manager 無理かな...半ば諦めていたところに、なんと Remote Desktop で繋げられたという記事を発見した。
これはマジですごい。 Linux 上で `aws ssm start-session` を `strace` して解析したらしい。鬼テク。

[Remote Access to Windows EC2 instances, the easy (and secure) way](https://cloudsoft.io/blog/remote-access-to-windows-ec2-instances-the-easy-and-secure-way)

解析結果から作成され接続確認された PowerShell モジュールが GitHub に公開されている。

[EC2Access/EC2Access.psm1 at v1.0.0 · cloudsoft/EC2Access](https://github.com/cloudsoft/EC2Access/blob/v1.0.0/EC2Access.psm1#L170-L243)

元記事は RemoteDesktop を対象としているが、ぱっと見 SSH でも同様のことができそう。
コミットが 1 年半くらい前なので Session Manager の API に変更がなければ、やけど。
Session Manager plugin にパラメータを渡す部分を参考に、接続を試行してみる。
[ライセンスは Apache License 2](https://github.com/cloudsoft/EC2Access/blob/v1.0.0/LICENSE) なので無償利用・改変もヨシ。

試してみる。

---

まず AWS CLI の場合だが以下のような感じで使ってた。
見やすさのために改行成分多く含んでいる。

```powershell
Get-EC2Instance -Filter @{
    Name = 'tag:Name'
    Values = 'instance-name'
} | Select-Object -ExpandProperty Instances -First 1 | ForEach-Object {
    aws ssm start-session --target $_.InstanceId # ここを変えたいんや
}
```

これを一先ずこんな風に使えるような関数を作成してみてはどうか？(`ForEach-Object` 要らんが SSH は単発でつなぎたいし一先ず)。

```powershell
Get-EC2Instance -Filter @{
    Name = 'tag:Name'
    Values = 'instance-name'
} | Select-Object -ExpandProperty Instances -First 1 | ForEach-Object {
    Start-SSMSshSession -Instance $_.InstanceId # こういう関数がほしい
}
```

先述の [EC2Access.psm1](https://github.com/cloudsoft/EC2Access/blob/v1.0.0/EC2Access.psm1#L170-L243) からコードを拝借する。
`Start-SSMSession` の戻り値をこねくる部分と、 Session Manager plugin を取り扱う箇所が対象だ。
コメントも処理の解析に有用なのでそのまま残させていただく。

元コードは RemoteDesktop を対象にしているので、認証情報の作成、 port forwarding 、あと最後の接続の箇所だけ SSH 用に書き換える。

```powershell
function Start-SSMSshSession {
    [CmdletBinding(SupportsShouldProcess)]
    param (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]
        $InstanceId,
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $Region = 'ap-northeast-1',
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $SshUser = 'ec2-user',
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]
        $CertPath,
        [Parameter()]
        [int]
        $LocalPort = 10022
    )

    $PortForwardParams = @{ portNumber = (, '22'); localPortNumber = (, $LocalPort.ToString()) }
    $session = Start-SSMSession -Target $InstanceId -DocumentName 'AWS-StartPortForwardingSession' -Parameters $PortForwardParams

    # We now need to emulate awscli - it invokes session-manager-plugin with the new session information.
    # AWS Tools for PowerShell don't do this. Also some of the objects seem to look a bit different, and the
    # plugin is pernickety, so we have to jump through some hoops to get all the objects matching up as close
    # as we can.

    $SessionData = @{
        SessionId = $session.SessionID;
        StreamUrl = $session.StreamUrl;
        TokenValue = $session.TokenValue;
        ResponseMetadata = @{
            RequestId = $session.ResponseMetadata.RequestId;
            HTTPStatusCode = $session.HttpStatusCode;
            RetryAttempts = 0;
            HTTPHeaders = @{
                server = 'server';
                'content-type' = 'application/x-amz-json-1.1';
                'content-length' = $session.ContentLength;
                connection = 'keep-alive';
                'x-amzn-requestid' = $session.ResponseMetadata.RequestId;
            }
        }
    }

    $RequestData = @{
        Target = $InstanceId;
        DocumentName = 'AWS-StartPortForwardingSession';
        Parameters = $PortForwardParams
    }

    $Arguments = (
        (ConvertTo-Json $SessionData -Compress),
        $Region,
        'StartSession',
        '',
        (ConvertTo-Json $RequestData -Compress),
        "https://ssm.$($Region).amazonaws.com"
    )

    # Now we have to do some PowerShell hacking. Start-Process takes an array of arguments, which is great,
    # but it doesn't actually do what we expect it to - see https://github.com/PowerShell/PowerShell/issues/5576.
    # So instead we have to turn it into an escaped string ourselves...
    $EscapedArguments = $Arguments | ForEach-Object { $escaped = $_ -replace "`"", "\`""; "`"$($escaped)`"" }
    $ArgumentString = $EscapedArguments -join ' '

    # Start the Session Manager plugin:
    if ($PSCmdlet.ShouldProcess($session.SessionId, 'Start Session Manager plugin')) {
        try {
            $Process = Start-Process -FilePath 'session-manager-plugin.exe' -ArgumentList $ArgumentString -NoNewWindow -PassThru
        }
        catch {
            Write-Error 'Unable to start the process session-manager-plugin.exe. Have you installed the Session Manager Plugin as described in https://docs.aws.amazon.com/systems-manager/latest/userguide/session-manager-working-with-install-plugin.html#install-plugin-windows ?'
            exit
        }

        # Wait a moment for it to connect to the session and open up the local ports
        Start-Sleep -Seconds 1

        # The port should be open now - let's connect
        if ($PSCmdlet.ShouldProcess($InstanceId, 'Start SSH client')) {
            ssh "${SshUser}@127.0.0.1" -p $LocalPort -i $CertPath
        }

        # Once the ssh session has finished, kill the session manager plugin
        $Process.Kill()
    }
}
```

`aws ssm start-session` だと `ssm-user` でつなぐことになるが、自前で接続する場合このユーザの認証情報がわからなかった。
代わりに `ec2-user` を使い、合わせて鍵も渡すようにした。
結果、当初の想定よりパラメータが増えてしまうが、致し方なし。

できたら試す。 IP 等の一部の情報は伏せ字とする。

```powershell
PS> Get-EC2Instance -Filter @{
>     Name = 'tag:Name'
>     Values = 'instance-name'
> } | Select-Object -ExpandProperty Instances -First 1 | ForEach-Object {
>     Start-SSMSshSession -InstanceId $_.InstanceId -CertPath ~/.ssh/instance-name.pem
> }

Starting session with SessionId: krymtkts-031e957031cdcb25c
Port 10022 opened for sessionId krymtkts-031e957031cdcb25c.
Waiting for connections...

Connection accepted for session [krymtkts-031e957031cdcb25c]
Last login: Sun Feb 19 06:09:45 2023 from localhost

       __|  __|_  )
       _|  (     /   Amazon Linux 2 AMI
      ___|\___|___|

https://aws.amazon.com/amazon-linux-2/
22 package(s) needed for security, out of 22 available
Run "sudo yum update" to apply all updates.
[ec2-user@ip-xxx-xxx-xxx-xxx ~]$ exit
logout
Connection to 127.0.0.1 closed.
```

見事つなげる事ができた。すーご。

非公式とはいえ、 AWS CLI だけでしかできかなったことが PowerShell でもできるというのがわかるのは、一ユーザとして喜ばしいこっちゃな。
[元記事](https://cloudsoft.io/blog/remote-access-to-windows-ec2-instances-the-easy-and-secure-way) の Great work に感謝やな。

[^1]: [(Optional) Install the Session Manager plugin for the AWS CLI - AWS Systems Manager](https://docs.aws.amazon.com/systems-manager/latest/userguide/session-manager-working-with-install-plugin.html) 入れておけばというか入れてないと無理
[^2]: [Support for interactive Start-SSMSession · Issue #283 · aws/aws-tools-for-powershell](https://github.com/aws/aws-tools-for-powershell/issues/283#issuecomment-1247377153) で(AWS Tools for PowerShell は)外部プログラムに依存できないし、 Session Manager plugin は AWS CLI 専用て言ってる
