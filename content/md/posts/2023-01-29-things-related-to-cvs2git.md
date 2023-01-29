{:title "cvs2git にまつわるアレコレ"
:layout :post
:tags ["cvs2git","docker"]}

ちょっとだけ CVS から Git に変換するときの事がわかった。

[先日の日記](/posts/2023-01-22-want-to-comvert-migu-cvs-to-git-and-failed)にも追記したが、 remote repo の本体にファイルアクセスできないと使えない。それはつまり repo 所有者じゃないと git repo への変換ができないんだ。
もうこれだけで、自分で手を動かしてどうこうする計画は終了やなというところだが、なんか調べたことを供養するために記しておく。

---

cvs2svn は今や GitHub repo を残すのみ。多くのドキュメントに参照されているであろう `http://cvs2svn.tigris.org/` はもう閉鎖されている。
[mhagger/cvs2svn: Migrate CVS repositories to Subversion or Git. This site supersedes the old tigris.org site, which has shut down.](https://github.com/mhagger/cvs2svn)

で、この GitHub repo に cvs2git 含む cvs2svn を動かすための Dockerfile もある。
わたしはそれを知る前に、 Ubuntu 18 であればパッケージが提供されてるのを知ったのでとりあえずそれを使う Dockerfile を書いたのだけど、本家を使う方が良いだろう。

書いた Dockerfile の repo は供養のため public にした。[krymtkts/ubuntu-cvs2git](https://github.com/krymtkts/ubuntu-cvs2git)

更に、 PowerShell でこの Docker image をビルドし実行するにあたって、どうでもいい気付きを得た。

PowerShell で Docker の `--mount` オプションを使う場合は、オブションに渡すパラメータ全体を文字列にしてやる必要があった。
昔は `--volume` 使ってたし、ビルド関係は `COPY` で済ますし、何なら最近触ってなかったから気づきになって良かったわ。

[Bind mounts | Docker Documentation](https://docs.docker.com/storage/bind-mounts/#start-a-container-with-a-bind-mount) には bash かなんかの shell の例があるが、これは PowerShell では NG 。

> ```sh
>  docker run -d \
>   -it \
>   --name devtest \
>   --mount type=bind,source="$(pwd)"/target,target=/app \
>   nginx:latest
> ```

PowerShell の流儀に従えばこうなる。

```pwsh
docker run -d `
  -it `
  --name devtest `
  --mount "type=bind,source=$((pwd).Path)/target,target=/app" `
  nginx:latest
```

因みに、 Windows の場合は Path の delimiter が `\` なので `/` に変換する必要がある、という情報を Web でよく見る。
のだけど、 `\` `/` にかかわらずマウントできた(更新されて変わったのかな)。

```pwsh
# こうしてもしなくてもどっちでもよい
docker run -d `
  -it `
  --name devtest `
  --mount "type=bind,source=$((pwd).Path -replace '\\','/')/target,target=/app" `
  nginx:latest
```

---

今回調べたことで改めて知ったが、CVS/SVN -> Git の変換はもうみんな興味なく、情報は陳腐化してるしツールは放棄されかかってるようだった。
作ってる人含め誰も使わなくなったらまあそうなるわ、という感じ。
でもその変換の過渡期に行動を起こさなかった repo てまだありそう。頭の片隅においておこう。
