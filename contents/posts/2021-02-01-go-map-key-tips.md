---
title: "go の map のキーの Tips"
tags: ["go"]
---

- 知ってた
  - `map`のキーには`struct`が使える
  - キーがポインタやポインタを含む`struct`の場合、ポインタの指す値が同じでもポインタ値自体が比較されるため異なるキーとなる
- 知らなかった
  - `interface`もキーにできる
    - キーを追加するときにポインタを使ってないこと
  - `time.Time`はキーにつこたらあかん
    - [src/time/time.go - go - Git at Google](https://go.googlesource.com/go/+/go1.15.6/src/time/time.go#147)
    - [time · pkg.go.dev](https://pkg.go.dev/time#Time)
      - > Note that the Go == operator compares not just the time instant but also the Location and the monotonic clock reading. Therefore, Time values should not be used as map or database keys without first guaranteeing that the identical Location has been set for all values, which can be achieved through use of the UTC or Local method, and that the monotonic clock reading has been stripped by setting t = t.Round(0). In general, prefer t.Equal(u) to t == u, since t.Equal uses the most accurate comparison available and correctly handles the case when only one of its arguments has a monotonic clock reading.
    - UT では一致しちゃいバグを見つけられなかった...😥
    - まだわかってないのが、`Location`が必ず同じになるようにしてた＆年月日だけの情報しか持ってなかったのにずれとんのかい！？というところ

[コード](https://play.golang.org/p/Rd5OJ2S37AT)。このコードでは`time.Time`のキーのズレは再現しないけど。

```go
package main

import (
	"fmt"
	"time"
)

type Key interface {
}

type KeyA struct {
	s string
}

type KeyB struct {
	s string
	i int
}

type KeyC struct {
	t time.Time
}

type KeyD struct {
	a *KeyA
}

func checkInterfaceKey() {
	m := map[Key]int{}
	m[KeyA{
		s: "A1",
	}] = 1
	m[KeyA{
		s: "A1",
	}] = 2
	m[KeyA{
		s: "A2",
	}] = 3
	m[KeyB{
		s: "B1",
		i: 1,
	}] = 4
	m[KeyB{
		s: "B1",
		i: 1,
	}] = 5
	m[KeyB{
		s: "B1",
		i: 2,
	}] = 6
	fmt.Printf("%v\n", m)
}

func init() {
	location := "Asia/Tokyo"
	loc, err := time.LoadLocation(location)
	if err != nil {
		loc = time.FixedZone(location, 9*60*60)
	}
	time.Local = loc
	fmt.Printf("%v\n", loc)
}

func checkPointerIncludedKey() {
	m := map[Key]int{}
	m[KeyC{
		t: time.Date(2021, 2, 1, 0, 0, 0, 0, time.Local),
	}] = 1
	m[KeyC{
		t: time.Date(2021, 2, 1, 0, 0, 0, 0, time.Local),
	}] = 2
	m[KeyD{
		a: &KeyA{
			s: "D1",
		},
	}] = 3
	m[KeyD{
		a: &KeyA{
			s: "D1",
		},
	}] = 4
	fmt.Printf("%v\n", m)
}

func main() {
	checkInterfaceKey()
	checkPointerIncludedKey()
}

```
