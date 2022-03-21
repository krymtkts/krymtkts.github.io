{:title "PowerShell で Docker Toolbox を使う"
:layout :post
:tags ["powershell", "docker", "docker-toolbox"]}

へーしゃはちょっといけてなくて、秘伝の古い VM をコンテナ化しない~~できない~~まま使っている。

加えて社内は Windows ユーザと Mac ユーザがごちゃまぜなので VM が VirtualBox のため、Windows ユーザであるわたしは Docker on Windows を使えず。

Docker Toolbox を日常使いしているのだが、`docker`に`docker-compose`に`docker-machine`まで現れたらコマンドやオプションが覚えきれないのが現状である 😭

これらを PowerShell で楽ちんに使おう。いますぐ以下のモジュールを PSGallery からゲットしよう。これらを発見したときは狂喜乱舞した。

- [PowerShell Gallery | DockerCompletion 1.1903.0.190723](https://www.powershellgallery.com/packages/DockerCompletion/1.1903.0.190723)
- [PowerShell Gallery | DockerComposeCompletion 1.24.0.190329](https://www.powershellgallery.com/packages/DockerComposeCompletion/1.24.0.190329)
- [PowerShell Gallery | DockerMachineCompletion 0.16.2.190903](https://www.powershellgallery.com/packages/DockerMachineCompletion/0.16.2.190903)

以下、雑なインストール例。もちろん PowerShell Guys(勝手にそう呼んでいる)なら自前の Profile に追記しよう。

```powershell
Install-Module -Name DockerCompletion,DockerComposeCompletion,DockerMachineCompletion
Import-Module -Name DockerCompletion,DockerComposeCompletion,DockerMachineCompletion
```

これで`docker-machine start`に始まり`docker`やら`docker-compose`でのイメージ・コンテナの名前補完とか諸々できるようになる。世の中、すごいモジュールを作る方がいるもんやねえ 🤔
