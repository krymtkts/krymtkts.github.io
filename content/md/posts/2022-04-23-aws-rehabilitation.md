{:title "AWS リハビリのハマり集"
:layout :post
:tags ["aws","powershell"]}

現職は半年ぶりの AWS ということで、ちまちまリハビリをしている。
諸々忘れてたりものによってはハマっている現状。記憶に定着させるため、ハマったところを記す。

### Hello World 以外の `sam init` できません

AWS SAM は前職でも使っていたが、久しぶりに触ったら詰まるところがあったので書いておく。

久しぶりに `sam init` すると 100%エラーで終わる。端末は Windows 11。
こんな風に ↓。

```log
Cloning from https://github.com/aws/aws-sam-cli-app-templates (process may take a moment)
Error: Can't find application template quick-start-web - check valid values in interactive init.
```

[Unable to create Serverless API using SAM v1.40.0 · Issue #3692 · aws/aws-sam-cli](https://github.com/aws/aws-sam-cli/issues/3692#issuecomment-1070222512)

どうも Windows 限定の問題らしい。 `sam` が使う template repository を `git clone` できないという深刻なやつ。
AWS SAM の一時ディレクトリを消して、手動で `git clone` したら解消する。これは Issue は Closed になっているものの、中々気付かんし原因を究明したい気がする。

### MFA 有効化されたアクセスキーの使い方忘れた

タイトルの通り。

そのまま使うとワンタイムパスの入力を求められないが、一時クリデンシャルを発行するときに出る。

昔は MFA が必要な switch role するときに aws-mfa を使っていたが、 この度 AWS Tools for PowerShell で自分の利用ケースのみシンプルにこしらえた。
ワンタイムパスワードの入力には 1Password の CLI である [`op`](https://developer.1password.com/docs/cli/get-started) を使うと究極に楽。現職で 1Password を使ってるので導入したみた。

```powershell
$env:AWS_REGION = 'ap-northeast-1'
$user = 'krymtkts'
$AWSLogin = 'AWS'
$c = Get-STSSessionToken -SerialNumber (Get-IAMMFADevice -UserName $user).SerialNumber -TokenCode (op item get $AWSLogin --otp) -ProfileName $user
$env:AWS_ACCESS_KEY_ID = $c.AccessKeyId
$env:AWS_SECRET_ACCESS_KEY = $c.SecretAccessKey
$env:AWS_SESSION_TOKEN = $c.SessionToken
```

楽～。これは PowerShell の profile に関数書いておいた。

### AWS Tools for PowerShell は自動補完で一覧する profile を絞り込んでる

`.aws/credentials` にこういう profile があるとする。これ、 `mfa_serial` が紛れ込んでる。

```ini
[profile]
aws_access_key_id = XXXXXXXXXXXXXXXXXXXX
aws_secret_access_key = XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
mfa_serial = xxx
```

AWS CLI のタブ補完コマンド `aws_completer` はこういうのがあっても問題なく profile を一覧できるが、 AWS Tools for PowerShell では一覧されなくなる。 `mfa_serial` 以外には `role_arn`, `source_profile` があると一覧されなくなる。

これ初めて知った。今後は、書き間違ってるから一覧されないんだ～と思いつくことができるが、一発目だったのでとても時間がかかった。ドキュメントに書いてる挙動なんかな～しらんけど。今度調べたい(いつ)。

続くかも。
