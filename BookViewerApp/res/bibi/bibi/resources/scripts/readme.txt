以下の部分を修正しています。
/^https?:\/\//.test(i)?"URI":"Base64"
→
/^https?:\/\//.test(i)||/^ms-local-stream:\/\//.test(i)?"URI":"Base64"

このファイルはサーバー上に配置されることを想定していますが、ビュワーで開く場合は"ms-local-stream://"で始まるURLになります。
Base64扱いされてまともに動作しないので修正しました。

ファイル更新時には注意してください。
