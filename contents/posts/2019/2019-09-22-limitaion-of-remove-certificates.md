---
title: "ALBのListenerから証明書を削除するときの制限"
tags:  ["aws", "elb", "acm"]
---

タイトルの通り。

仕事中にわかっただけで2つある。

1. ALBのListenerに登録された証明書を一度にまとめて消せる上限は10件ぽい
2. ALBのListenerから証明書を消したあと、証明書に関連づいたリソースが消えるまで待たないといけない

上記はPython(boto3)からAWSのリソースを操作した際のもの。boto3はpipでインストールできる最新のv1.9.233。

### 1. ALBのListenerに登録された証明書を一度にまとめて消せる上限は10件ぽい

ドキュメントには書かれてない気がする。少なくとも[boto3](https://boto3.amazonaws.com/v1/documentation/api/latest/reference/services/elbv2.html#ElasticLoadBalancingv2.Client.remove_listener_certificates)と[ELBのAPIリファレンス](https://docs.aws.amazon.com/elasticloadbalancing/latest/APIReference/API_RemoveListenerCertificates.html)のどちらにも記載がない。

問い合わせして聞いてないねんけど、書いてあったら教えてほしい🤔

以下はListener Certificatesの上限25件全部を一気に消そうとしたときのログ。センシティブな部分は削ってある。

```plaintext
Traceback (most recent call last):
  File "C:\workspace\alb-batch\albbatch\elbapi.py", line 102, in remove_cerificates
    Certificates=params)
  File "C:\Users\takatoshi_kuriyama\.virtualenvs\alb-batch-J4EQQ8Xf\lib\site-packages\botocore\client.py", line 357, in _api_call
    return self._make_api_call(operation_name, kwargs)
  File "C:\Users\takatoshi_kuriyama\.virtualenvs\alb-batch-J4EQQ8Xf\lib\site-packages\botocore\client.py", line 661, in _make_api_call
    raise error_class(parsed_response, operation_name)
botocore.exceptions.ClientError: An error occurred (ValidationError) when calling the RemoveListenerCertificates operation: Up to '10' certificate ARNs can be specified, but '25' were specified
```

### 2. ALBのListenerから証明書を消したあと、証明書に関連づいたリソースが消えるまで待たないといけない

ACMで証明書を削除する場合、その証明書が他のAWSリソースに関連付けられていると`ResourceInUseException`(boto3では`ClientError`)で削除できない。

[ACM.Client.delete_certificate](https://boto3.amazonaws.com/v1/documentation/api/latest/reference/services/acm.html#ACM.Client.delete_certificate) から抜粋。

> You cannot delete an ACM certificate that is being used by another AWS service. To delete a certificate that is in use, the certificate association must first be removed.

なのでまず関連付けられたLoad BalancerのListenerなどから証明書を取り除いた後、かつ証明書の情報からもその関連付けが取り除かれたことを確認しないと安全に削除できない😱

どうすればよいかというと、ACMの`DescribeCertificate`のレスポンスに含まれる`InUseBy`リストの中が空になってたら、それらの関連付けが取り除かれた状態なので安全に削除できる。

ACMで提供されてる`Waiter`は検証待ちのみで、それ用にはないみたいなので、以下のように手動で待ち合わせする...🤔

```python
def wait_until_certificate_unused(self, arn: str):
    cert = self.acm_client.describe_certificate(CertificateArn=arn)
    while len(cert['InUseBy']) > 0:
        time.sleep(1)
        cert = self.acm_client.describe_certificate(CertificateArn=arn)
```

めんどくせえええええ、こんなsnipetレベルのモノは`Waiter`作って欲しいわ😭
