{:title "PSJobCanAttendance の公開準備"
:layout :post
:tags ["powershell"]}

[krymtkts/PSJobCanAttendance](https://github.com/krymtkts/PSJobCanAttendance) を PowerShell Gallery に公開する準備をしている。
一括編集機能に若干の改善をした。

従来は、パイプラインを使った一括編集が可能だったが、出勤とか退勤とかの打刻イベント毎にパイプを分ける必要があった。

```powershell
# 従来はこんなん
@(12..16;20..22) | %{get-date "2022-09-$($_) 08:15:00+0900"} | Edit-JobCanAttendance -TimeRecordEvent work_start -AditGroupId 10
@(12..16;20..22) | %{get-date "2022-09-$($_) 12:00:00+0900"} | Edit-JobCanAttendance -TimeRecordEvent rest_start -AditGroupId 10
```

これ自分で使ってみて結構面倒に感じて、時刻と打刻イベントをパイプラインで渡せるように調整した。
この調整により、 1 ヶ月分の出勤・休憩の開始＆終了を一気に登録できる様になってしまった。怠惰が捗る(打刻を必要とする勤怠管理なのは触れてはならない)。

```powershell
# 1 ~ 31 日の間で 7 日、 10 日、土日を除いた日に出勤と休憩開始・終了を一括登録する。
1..31 | ? {$_ -notin 7,10 } | % {get-date -Day $_} | ? -Property DayOfWeek -notin 0,6 | % {
    [PSCustomObject]@{
        TimeRecordEvent='work_start'
        RecordTime= Get-Date -Date $_ -Hour 8 -Minute 15 -Second 0
    }
    [PSCustomObject]@{
        TimeRecordEvent='rest_start'
        RecordTime= Get-Date -Date $_ -Hour 12 -Minute 0 -Second 0
    }
    [PSCustomObject]@{
        TimeRecordEvent='rest_end'
        RecordTime= Get-Date -Date $_ -Hour 13 -Minute 0 -Second 0
    }
} | Edit-JobCanAttendance -AditGroupId 10 -Verbose​
```

これには [ValueFromPipelineByPropertyName](https://learn.microsoft.com/ja-jp/dotnet/api/system.management.automation.parameterattribute.valuefrompipelinebypropertyname?view=powershellsdk-1.1.0) を使った。
Cmdlet のパラメータにパイプラインの入力オブジェクト(`hashtable` は No)が持つ同名プロパティをマッピングできるので、複数のパラメータをバインドできる。
これ自分の書いた Cmdlet で使うことなかったので、いい勉強になった。

その後 PSScriptAnalyzer の指摘を修正して、さて公開しよう、というところで登録済みの勤怠を一覧するところにバグが見つかった。
登録した日と勤怠の実績がずれるバグで、これは直したいな～というやつなので、公開はまた先送り・未遂に終わった。

続く。

---

あと別の文脈で、放置していた[krymtkts/PSMFAttendance](https://github.com/krymtkts/PSMFAttendance)にメンテの予定がないよとアナウンスを追記した。これで Archive する準備も整った(まだしてない)。

これまた別の文脈で、現職では Slack に日報を投稿してから退勤するようにしてるので、そこから投稿時間を拾って退勤実績を作るようにしたいと考えている。
そのために Slack からメッセージの一覧を取得し、 PSJobCanAttendance での一括編集にもっていきたい。

過去に似たようなことをしたとき、 [RamblingCookieMonster/PSSlack](https://github.com/RamblingCookieMonster/PSSlack) を使った。
しかし PSSlack は 2021 年頃の Slack API の変更に追いついてないので、使うには自力でパッチする必要がある。
(パッチは来てるが適用されていない [Fix API deprecation 2021-02-24 by simonfagerholm · Pull Request #115 · RamblingCookieMonster/PSSlack](https://github.com/RamblingCookieMonster/PSSlack/pull/115))。

以前使った時はそうしたが、面倒なので .NET の Slack のモジュールとか探してなんとかしたい。
でも、どのモジュールがいいんか...と調べるのも面倒なので、悩み中。
