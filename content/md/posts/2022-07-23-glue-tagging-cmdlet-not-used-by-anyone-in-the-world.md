{:title "世界中の誰にも使われていない？ Glue のタグ付け Cmdlet"
:layout :post
:tags ["powershell","aws"]}

現職にて AWS のコストを可視化すべく、既存の AWS リソースにひたすらコスト配分タグを付与していく作業をしている(勝手に)。
Cost Explorer を眺めてコストのかかり具合を見るのは、 AWS の醍醐味というか楽しみだと思ってるのだけど、あんまり共感を得られたことがない。

タグ付けには[タグエディタ](https://docs.aws.amazon.com/ARG/latest/userguide/tag-editor.html)を使ってしまっても良いのだけど、折角なので、何を実行したかを形に残せしたい。
そこで [AWS Tools for PowerShell](https://aws.amazon.com/powershell/) でやってる[^1]のだけど、とあるサービスだけなんか他のサービスのタグ付け Cmdlet は作法が違ってた。

Glue 君のことね。 [Glue: Add-GLUEResourceTag Cmdlet | AWS Tools for PowerShell](https://docs.aws.amazon.com/powershell/latest/reference/items/Add-GLUEResourceTag.html)

この御方なんか知らんけど `Tag[]` じゃなくて `hashtable` を受けつける。コレまた当然のごとく Reference にはサンプルコード載ってないので、念のためググるかーと思いググったところ...

![9 件しかヒットしない Add-GLUEResourceTag](/img/2022-07-23-capture/aws-tools-for-pwsh.png)

9 件しかヒットしなかった(2022-07-22 時点)。マジで？
世界中で誰も使ってないのか、はたまたショボ過ぎて誰も記事にしないのか。

因みに使い方には困ることもなく、実行した結果も期待通り、ちゃんとタグが付いてた。

```powershell
$AccountId = Get-STSCallerIdentity | Select-Object -ExpandProperty Account
$Region = Get-DefaultAWSRegion | Select-Object -ExpandProperty Region
$Tag = @{
    'service' = 'my-service'
    'stage' = 'dev'
    'cost' = 'my-service-dev'
}
Get-GLUECrawlerList | ForEach-Object {
    # Glue Crawler の ARN が `Get-GLUECrawler` から持ってこれなくて、みすぼらしく手で組んでる。
    Add-GLUEResourceTag -ResourceArn "arn:aws:glue:${Region}:${AccountId}:crawler/$($_.Name)" -TagsToAdd $Tag -WhatIf
}
```

[AWS Glue でリソースタグがサポートされて 3 年ばかり経ってる](https://aws.amazon.com/jp/about-aws/whats-new/2019/03/aws-glue-now-supports-resource-tagging/)けどみんな使ってないのかなーとか、他にも使われてない Cmdlet があるんやろなーと、思いを馳せた。

AWS CLI の方はというと `"aws glue tag-resource"` で 3 件しかヒットしなかった。

![3 件しかヒットしない "aws glue tag-resource"](/img/2022-07-23-capture/aws-cli.png)

しかしながらヒットした記事は実行例が分かるものになってて、こう...なんか AWS Tools for PowerShell の人気の無さが際立つなーと感じた。

### 他を見る

雑だが、 AWS Tools for PowerShell のリソースタグ関連の Cmdlet で、タグのパラメータの型が何か調べるスクリプトを書いた。 CRUD 全部の Cmdlet が含まれるのであまり正確ではない。

わたしは AWS Tools for PowerShell の特定のモジュールしか入れてないから全体はわからんけど、傾向は見られる。

```powershell
# AWS.Tools.* modules must be imported first to get all property information.
Get-Module -Name *AWS* | Import-Module
Get-Command *Tag* | Where-Object -Property Source -Like '*aws*' | ForEach-Object {
    [pscustomobject]@{
        Name = $_.Name
        TagParameterType = $_.Parameters.Values | Where-Object -Property Name -Like '*tag*' | Select-Object -ExpandProperty ParameterType
    }
} | Where-Object -Property TagParameterType -NE $Null | Group-Object TagParameterType

# Count Name
# ----- ----
#    31 System.String[]
#     8 Amazon.IdentityManagement.Model.Tag[]
#     4 System.Collections.Hashtable
#     3 {System.String[], System.String[]}
#     3 Amazon.CodeDeploy.Model.Tag[]
#     3 System.String
#     2 Amazon.OpenSearchService.Model.Tag[]
#     2 Amazon.AutoScaling.Model.Tag[]
#     2 Amazon.EC2.Model.Tag[]
#     2 Amazon.S3.Model.Tag[]
#     2 Amazon.Redshift.Model.Tag[]
#     2 Amazon.CertificateManager.Model.Tag[]
#     1 Amazon.ECS.Model.Tag[]
#     1 Amazon.CloudWatch.Model.Tag[]
#     1 Amazon.SimpleNotificationService.Model.Tag[]
#     1 Amazon.StepFunctions.Model.Tag[]
#     1 Amazon.SecretsManager.Model.Tag[]
#     1 Amazon.ECR.Model.Tag[]
#     1 Amazon.RDS.Model.Tag[]
#     1 Amazon.KeyManagementService.Model.Tag[]
#     1 Amazon.KinesisFirehose.Model.Tag[]
#     1 Amazon.DynamoDBv2.Model.Tag[]
#     1 Amazon.ECR.ImageTagMutability
#     1 Amazon.ElasticLoadBalancingV2.Model.Tag[]
#     1 Amazon.ElastiCache.Model.Tag[]
#     1 Amazon.QuickSight.Model.Tag[]
#     1 Amazon.EventBridge.Model.Tag[]
```

`*.Tag[]` が多数派かなーと。 `31 System.String[]` これは多分削除とか取得かな。
ただしこいつら `*.Tag[]` グループの中でも、 プロパティが `Key` だったり `TagKey` だったりの派閥があるので、ほんま「みんな違ってみんないい」的状態。

おわり。

[^1]: 既存リソースは IaC じゃないので、コマンドでタグ付けするため。
