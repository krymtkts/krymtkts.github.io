---
title: NuGet の Dependabot で project が見つからないエラーを直した
tags: [dotnet, github]
---

2025-02E 頃から .Net (NuGet) の [Dependabot version updates](https://docs.github.com/en/code-security/dependabot/dependabot-version-updates/about-dependabot-version-updates) が失敗するようになって、なんでかわからんしそのうち直るかなと放置していた。
ただ全然直らないし、 [krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) でもエラーになってるので、最近になってようやく重い腰を上げて調べてみたところ、以下のようなエラーが出いていた。例は [krymtkts/pocof](https://github.com/krymtkts/pocof) のもの。

[nuget in /. - Update #992720506 · krymtkts/pocof@29eff1c](https://github.com/krymtkts/pocof/actions/runs/14252933221/job/39949755290)

```plaintext
2025/04/03 21:16:53 INFO Temporarily removing `global.json` from `/home/dependabot/dependabot-updater/repo`.
2025/04/03 21:16:53 INFO Restoring `global.json` to `/home/dependabot/dependabot-updater/repo`.
2025/04/03 21:16:53 INFO Discovering build files in workspace [/home/dependabot/dependabot-updater/repo].
2025/04/03 21:16:53 INFO   Discovered [.config/dotnet-tools.json] file.
2025/04/03 21:16:53 INFO   Discovered [global.json] file.
2025/04/03 21:16:53 INFO   Discovering projects beneath [.].
2025/04/03 21:16:53 INFO   No project files found.
2025/04/03 21:16:53 INFO Discovery complete.
updater | 2025/04/03 21:16:53 INFO <job_992720506> Discovery JSON content: {
  ... 長いので省略 ...
}
updater | 2025/04/03 21:16:53 INFO <job_992720506> Discovery JSON path for workspace path [/] found in file [/home/dependabot/.dependabot/discovery_map.json] at location [/home/dependabot/.dependabot/discovery.1.json]
updater | 2025/04/03 21:16:53 INFO <job_992720506> Discovery JSON path for workspace path [/] found in file [/home/dependabot/.dependabot/discovery_map.json] at location [/home/dependabot/.dependabot/discovery.1.json]
updater | 2025/04/03 21:16:53 ERROR <job_992720506> Error during file fetching; aborting: Repo must contain .(cs|vb|fs)proj file.
  proxy | 2025/04/03 21:16:53 [024] POST /update_jobs/992720506/record_update_job_error
  proxy | 2025/04/03 21:16:53 [024] 204 /update_jobs/992720506/record_update_job_error
  proxy | 2025/04/03 21:16:53 [026] PATCH /update_jobs/992720506/mark_as_processed
  proxy | 2025/04/03 21:16:54 [026] 204 /update_jobs/992720506/mark_as_processed
updater | 2025/04/03 21:16:54 INFO <job_992720506> Finished job processing
updater | 2025/04/03 21:16:54 INFO Results:
Dependabot encountered '1' error(s) during execution, please check the logs for more details.
+---------------------------+
|          Errors           |
+---------------------------+
| dependency_file_not_found |
+---------------------------+
Failure running container 8fc2652dfc6bfe10bbe91cc64e0fa63347c0031374edc8419b45c3ce073dcff5
Cleaned up container 8fc2652dfc6bfe10bbe91cc64e0fa63347c0031374edc8419b45c3ce073dcff5
  proxy | 2025/04/03 21:16:55 3/13 calls cached (23%)
  proxy | 2025/04/03 21:16:55 Posting metrics to remote API endpoint
Error: Dependabot encountered an error performing the update

Error: The updater encountered one or more errors.

For more information see: https://github.com/krymtkts/pocof/network/updates/992720506 (write access to the repository is required to view the log)
🤖 ~ finished: error reported to Dependabot ~
```

`No project files found.` というログやら `dependency_file_not_found` というエラーがでて `fsproj` が見つからなくなってる。
このとき `dependabot.yml` はこうしていた。

```yml
# Maintain dependencies for NuGet
- package-ecosystem: "nuget"
  directory: "/"
```

エラー発生以前は、この設定で指定した `directory` 配下に対して再帰的に `fsproj` を検索できていたという認識だが。
ただ Copilot や ChatGPT に聞いてもそんなことはできないという。
そこで Dependabot の文書をあたってみたところ、今回初めて知ったのだが、 `directory` は project などの依存関係を管理するファイルが直に配置されてないといけないようだ。

[Dependabot options reference - GitHub Docs](https://docs.github.com/en/code-security/dependabot/working-with-dependabot/dependabot-options-reference#directories-or-directory--)

> Use to define the location of the package manifests for each package manager (for example, the package.json or Gemfile).

エラー発生以前の、 `directory` 配下に対して再帰的に `fsproj` を検索できていたのが、むしろ間違っていたと。
なんてこったい。

krymtkts/pocof のように複数のディレクトリに依存関係を管理するファイルが配置されている場合は、複数指定できる `directories` を使うと良いそうだ。
また、 `directories` では [glob pattern](<https://en.wikipedia.org/wiki/Glob_(programming)>) が使える。
それにより pocof では結果的に以下のように 1 要素だけの定義で済んだ。
krymtkts/SnippetPredictor も同様に対処した。

```yml
# Maintain dependencies for NuGet
- package-ecosystem: "nuget"
  # NOTE: dependabot raises dependency_file_not_found error with root directory.
  # NOTE: directories can use glob patterns.
  directories:
    - "/src/pocof*"
```

最後に、これはこの先絶対忘れそうだなということで、備忘のための note も残した。

再帰的に検索するほうが便利よなと考えるが、仕様に準拠してないとなればまあ直すよなという感じ。
他の package manager 用の Dependabot と挙動が違うようでは尚更。
何にせよ、自身の間違いに気づき、エラーが解決できてよかった。
