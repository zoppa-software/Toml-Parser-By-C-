# Toml-Parser-By-Csharp

## Overview
TOMLファイルを　**C#言語** で読み込み、値を取得するためのライブラリです。
私の Github公開のための学習用ライブラリです。開発は Visual Studio 2017で行っています。
ライセンスは MIT_license としています。

TOMLは initファイルと似た構文で設定情報を記述するための軽量な言語です。
以下に例を示します。詳細の仕様は[公式サイト](https://github.com/toml-lang/toml)を参照ください。

```
title = "TOML sample"

[database]
server = "192.168.1.1"
ports = [ 8001, 8002, 8003 ]
connection_max = 5000
enabled = true

[servers]

  # Indentation (tabs and/or spaces) is allowed but not required
  [servers.alpha]
  ip = "10.0.0.1"
  dc = "eqdc10"

  [servers.beta]
  ip = "10.0.0.2"
  dc = "eqdc10"

[clients]
data = [ ["gamma", "delta"], [1, 2] ]

# Line breaks are OK when inside arrays
hosts = [
  "alpha",
  "omega"
]
```

このライブラリでは以下のようにTOMLファイルを読み込みます。

``` C#
var tomlData = @"
    title = ""TOML sample""

    [database]
    server = ""192.168.1.1""
    ports = [8001, 8002, 8003]
    connection_max = 5000
    enabled = true

    [servers]
        # Indentation (tabs and/or spaces) is allowed but not required
        [servers.alpha]
        ip = ""10.0.0.1""
        dc = ""eqdc10""

        [servers.beta]
        ip = ""10.0.0.2""
        dc = ""eqdc10""

    [clients]
    data = [ [""gamma"", ""delta""], [1, 2] ]

    # Line breaks are OK when inside arrays
    hosts = [
        ""alpha"",
        ""omega""
    ]";

dynamic toml = new TomlDocument();
toml.Load(tomlData); // 上記文字列を取り込む。BOM無し UTF8で保存されたファイルも読み込める
AreEqual((string)toml.title, "TOML sample");

AreEqual((string)toml.database.server, "192.168.1.1");
AreEqual((int)toml.database.ports[0], 8001);
AreEqual((int)toml.database.ports[1], 8002);
AreEqual((int)toml.database.ports[2], 8003);
AreEqual((int)toml.database.connection_max, 5000);
AreEqual((bool)toml.database.enabled, true);

AreEqual((string)toml.servers.alpha.ip, "10.0.0.1");
AreEqual((string)toml.servers.alpha.dc, "eqdc10");
AreEqual((string)toml.servers.beta.ip, "10.0.0.2");
AreEqual((string)toml.servers.beta.dc, "eqdc10");

AreEqual((string)toml.clients.data[0][0], "gamma");
AreEqual((string)toml.clients.data[0][1], "delta");
AreEqual((int)toml.clients.data[1][0], 1);
AreEqual((int)toml.clients.data[1][1], 2);
AreEqual((string)toml.clients.hosts[0], "alpha");
AreEqual((string)toml.clients.hosts[1], "omega");
```

## Description
Tomlファイルを読み込むためのライブラリになります。
記述のしやすさから C#の #dynamic# による動的アクセスに対応しています。
ただし、#dynamic# ではインテリセンスによるコード補完、発生する例外がわかりにくく思います。
その場合は、ITomlValueインターフェイスで定義されているプロパティ、メソッドで値を取得してください。
オブジェクトのシリアライズを行う機能は現在検討中です。

## Licence
ライセンスは[MITライセンス](https://opensource.org/licenses/mit-license.php)としています。