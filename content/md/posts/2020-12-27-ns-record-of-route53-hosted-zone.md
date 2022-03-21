{:title "Rote53 ホストゾーンの NS レコード"
:layout :post
:tags ["powershell", "aws"]}

これで 12 月だいぶ躓いたので書いとくわ。思い出じゃ。

NS レコードは Simple Routing しかサポートしてへん。まずそれは AWS Management Console でわかる。
これを AWSPowerShell のパラメータでどうやんのかがわからんかった。ワタシが阿呆やからに違いない。

<script src="https://gist.github.com/krymtkts/7774bb65f2f0351697a47383aefe9ec9.js"></script>

はまったところ。

[change-resource-record-sets — AWS CLI 1.18.203 Command Reference](https://docs.aws.amazon.com/cli/latest/reference/route53/change-resource-record-sets.html)

> SetIdentifier -> (string)
>
> > > Resource record sets that have a routing policy other than simple: An identifier that differentiates among multiple resource record sets that have the same combination of name and type, such as multiple weighted resource record sets named acme.example.com that have a type of A. In a group of resource record sets that have the same name and type, the value of SetIdentifier must be unique for each resource record set.
>
> > For information about routing policies, see Choosing a Routing Policy in the Amazon Route 53 Developer Guide .

Alias レコードのパラメータからコピペしてたので`SetIdentifier`を残したままにしてしまっていた。NS レコードは Simple Routing のみを許可するのでエラーになるのね。↓

```powershell
  52 |  Edit-R53ResourceRecordSet -HostedZoneId $ParentHostedZone.Id -ChangeB …
     |  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
     | Invalid request: Expected exactly one of [Weight, Region, Failover, GeoLocation, or MultiValueAnswer], but found none in Change with [Action=CREATE, Name=test.testtest.com, Type=NS, SetIdentifier=nandeyanen]
```

わかりにくぅ！

おわり 😭
