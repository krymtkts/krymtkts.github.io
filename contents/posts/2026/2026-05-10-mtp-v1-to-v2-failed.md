---
title: "Microsoft.Testing.Platform (MTP) v1 to v2 出来なかった話"
tags: ["dotnet", "mtp"]
---

[pslrm Bump Action](https://github.com/marketplace/actions/pslrm-bump-action) が最低限整ったので、自分の PowerShell module の repo に順次導入した。
いつもそうだが、自分の作ったツールが定期実行や CI で元気に動いているのを見ると胸熱やな。
[sandbox](https://github.com/krymtkts/pslrm-actions-sandbox) における手動での検証以外ではまだ bump の PR が作成されたことがないので、そこが実運用で問題ないかは気になる点ではある。

まだ [pslrm](https://github.com/krymtkts/pslrm) でやりたいことはあるが、先述の既存 repo に対する pslrm 導入以外にも細かい整理をしている。
その 1 つが [Microsoft.Testing.Platform](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro)(MTP) v2 への移行だ。
結局未遂に終わったが、これはちょっと手間取ったので自分の理解の整理のためにも記録を残しておく。

---

.NET 素人のためちゃんと見てなかったが、 .NET 10 になって [`dotnet test`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test) に MTP mode が追加された。
これで実行モードが従来の VSTest mode と新しい MTP mode の 2 つになった。
この辺の記事を参照した。

- [Testing with 'dotnet test' - .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test)
- [Migrating from Microsoft.Testing.Platform (MTP) v1 to v2 - .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-migration-from-v1-to-v2)

.NET 10 の `dotnet test` で MTP v2 を MTP mode で使うには、利用者が [`global.json`](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json) で以下のように opt-in する必要がある。

```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

ただし、この利用者側の切り替えだけでは完結せず、test runner 提供側の対応も必要になる。

MTP v2 では .NET 10 上で VSTest-based の legacy な `dotnet test` 経路が使えなくなる。
そのため test runner 提供側は VSTest-based な実装を残すなら何らかの互換実装を持つ必要がある。
[`Microsoft.Testing.Extensions.VSTestBridge`](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-extensions-vstest-bridge) のような互換レイヤを使うか、自前の実装か。
ただしそれは既存の VSTest-based な実装を活かすための互換経路であって、内部実装まで pure native MTP に寄るわけではない。
そのため、test runner は native MTP 前提の提供・サポートへ寄せる判断を迫られる、ということらしい。

わたしの F# の project は概ね以下の構成になってる。

- test runner は [`Expecto`](https://github.com/haf/expecto)
  - MTP 対応のため [`YoloDev.Expecto.TestSdk`](https://www.nuget.org/packages/YoloDev.Expecto.TestSdk/) 0.15.6
    - [MTP v1.9.1](https://www.nuget.org/packages/Microsoft.Testing.Platform/1.9.1) に依存している
- coverage を取るのに [`coverlet.collector`](https://www.nuget.org/packages/coverlet.collector) or [`coverlet.console`](https://www.nuget.org/packages/coverlet.console)
  - `coverlet.collector` は VSTest 用なので `dotnet test` が VSTest mode で動く (pocof)
  - `coverlet.console` は `dotnet test` の外で動くので関係ない (SnippetPredictor/PSGameOfLife)

ここで問題になるのが、先述の v1/v2 の対応状況だ。

`YoloDev.Expecto.TestSdk` は [YoloDev/YoloDev.Expecto.TestSdk#263](https://github.com/YoloDev/YoloDev.Expecto.TestSdk/pull/263) 見る限りまだ MTP v2 対応が完了していない。
いま test project を MTP v2 にしてしまうと、 依存関係に MTP v1/v2 が混在してねじれが発生する。
これはたまたま動く状態にできるかも知れないが、運用不可が高いので避けたい。

ちなみに [Coverlet](https://github.com/coverlet-coverage/coverlet) の MTP 対応版は `coverlet.MTP` という package が出てる。
この今公開されてる版はどれも MTP v2 系で v1 系じゃあない。
なので MTP v1 採用なら `dotnet test` と一緒に VSTest mode で動かせる Coverlet は `coverlet.collector` になる。
逆に `coverlet.console` を使えば `dotnet test` の外で動かすので MTP の制約はなくなる。
ただし `coverlet.console` の coverage は実行モデルの違いにより `coverlet.collector` よりも正確じゃなくなるという肌感。

こういう状況なので、わたしの F# の project では `YoloDev.Expecto.TestSdk` に引きずられて MTP v1 系を維持したままにした。
でも pocof なんかは古い project 構成のため test project の main が残ってたりしたのでその掃除はしてある。
またこの機会に、テストが hang したときの dump も [Microsoft.Testing.Extensions.HangDump](https://www.nuget.org/packages/Microsoft.Testing.Extensions.HangDump) で VSTest から MTP に移行した。
これまでは [`--blame-hang-*`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-vstest) を使ってた。

今回整理する中で MTP v2 の MTP mode への移行は使ってる test runner によるから、利用者側もそんなに簡単じゃないなと思った。
どのみち VSTest/MTP v1/MTP v2 に単一 package で対応するのが難しかろうし、変わることを前提に別の解決案を模索するのも良いかも知れない。
