---
title: "boto3 の Waiter さんとの戯れ"
tags: ["python", "aws", "boto3"]
---

こないだの仕事。AWS Certificate Manager で証明書をごにょごにょするアプリを書いた。

Python で書いたので AWS のリソースを操作するのに [Boto3](https://boto3.amazonaws.com/v1/documentation/api/latest/index.html) というライブラリを利用した。証明書を発行したあと検証済みになるまでの待受処理が`Waiter`という機能で提供されていたり、非常に便利で大変お世話になっている。

- [ACM.Waiter.CertificateValidated — Boto 3 Docs 1.9.248 documentation](https://boto3.amazonaws.com/v1/documentation/api/latest/reference/services/acm.html#ACM.Waiter.CertificateValidated)

ただ、一つバグらせてしまったところがあった。

`Waiter`さんは AWS のリソースを操作する API をラップしているだけ[^1]なので、API がエラーを発生させた場合と待受処理がタイムアウトした場合のどちらとも、`WaiterError`を発生させる。つまり単純にキャッチするだけの例外処理では違いに気づけないのだ 😱

[^1]: 証明書の検証済みを待つ`ACM.Waiter.CertificateValidated`の場合は`ACM.Client.describe_certificate()`をラップしている。

どのようにハンドリングするか？`WaiterError`さんの属性を調べてあげれば良い。

`dir`したら`last_response`なる属性があったのでそいつを見たら、もとのエラーが何だったのかは分かる形にはなってた。

エラーの場合、`WaiterError.last_response['Error']`にエラー情報が格納される。`Waiter`さんがリトライ回数の上限に達して`WaiterError`をぶん投げてきた場合は、`last_response`には`Waiter`さんが内包する API の戻り値が架空されるので、それをもとにエラー処理すれば良いのがわかった。以下イメージ。

```python
{'Error': {'Message': 'Could not find certificate arn:aws:acm:ap-northeast-1:xxxxxxxxxxxx:certificate/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx in account xxxxxxxxxxxx.', 'Code': 'ResourceNotFoundException'}, 'ResponseMetadata': {'RequestId': 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx', 'HTTPStatusCode': 400, 'HTTPHeaders': {'x-amzn-requestid': 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx', 'content-type': 'application/x-amz-json-1.1', 'content-length': '191', 'date': 'Fri, 11 Oct 2019 03:51:10 GMT', 'connection': 'close'}, 'RetryAttempts': 0}}
```

👍
