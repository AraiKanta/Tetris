# AIにコーディング全部丸投げしてみたの巻
[APKインストールページ](https://github.com/KanSan00/Tetris/releases/tag/v1.0.0)
## 制作意図
なんと先日Googleさんが、AntiGravityなる人工知能ベースの統合開発環境を出したというではありませんか。Googleにサブスクしている私としては、使わない理由はない！ということで触ってみました。<br>
設定などはここで触れないので、こちらをご覧ください。
* [Google公式](https://codelabs.developers.google.com/getting-started-google-antigravity?hl=ja#0)
* [初期設定紹介ページ](https://cloud-ace.jp/column/detail532/)
## 感想
あの～。すっごい。はやい。えぐい。こちらの意図をしっかりとプロンプトに組み込むことでかなりの精度速度で回答を返してくるではありませんか。あ～らとてもいい子^^<br>
仕様書もAIで作らせて丸投げしてみたんです。完成後に再度プロジェクト全体を確認して、仕様書を完成させてくださいというお願いまで聞いてくれるではありませんか。
しかも、作業途中には実行計画書を作成して、「この計画で実行していいですか」と確認まで！素晴らしい８８８８８８８８８８８８８８８８８８８８８８８８８８<br>
設計書的なものも作ってくれているではありませんか！もうほんとになんていい子なのでしょう。<br>
コーディングだけ丸投げなので、UIやPrefab作ったりは自分でやりましたが、それはAIがコーディングしている間にやっていたのでほぼ同時並行！後々全部やらせてみたいもんですねｗ(どうやんだろうｗ)

---

# プロジェクト仕様書

## 1. 概要

本プロジェクトは、Unityで実装されたクラシックなテトリスゲームです。  
**MVP（Model-View-Presenter）アーキテクチャ**を採用し、VContainer、R3、UniTaskなどのモダンなライブラリを活用した設計となっています。

### 対応プラットフォーム
- PC（Windows / Mac）
- モバイル（Android / iOS）

---

## 2. アーキテクチャ

### 2.1 MVP設計パターン

```
┌─────────────────────────────────────────────────────────────┐
│                     VContainer (DI)                          │
│   GameLifetimeScope: Model/View/Presenterの依存関係を管理     │
└─────────────────────────────────────────────────────────────┘
                              │
       ┌──────────────────────┼──────────────────────┐
       ▼                      ▼                      ▼
┌─────────────┐      ┌──────────────┐      ┌─────────────┐
│   Model     │◄────►│  Presenter   │◄────►│    View     │
│  GameModel  │      │GamePresenter │      │  GameView   │
└─────────────┘      └──────────────┘      └─────────────┘
```

### 2.2 各層の責務

| 層 | クラス | 責務 |
|---|---|---|
| **Model** | `GameModel` | スコア、ライン数、グリッド状態のデータ管理 |
| **View** | `GameView`, `IGameView` | UI表示、ユーザー入力イベントの発火 |
| **Presenter** | `GamePresenter` | ModelとViewの仲介、ゲームロジックの実行 |

---

## 3. フォルダ構成

```
Assets/Scripts/
├── Models/          ← データ層
│   └── GameModel.cs
├── Views/           ← 表示層
│   ├── IGameView.cs (インターフェース)
│   └── GameView.cs
├── Presenters/      ← 仲介層
│   └── GamePresenter.cs
├── Core/            ← ゲームコア
│   ├── GameLifetimeScope.cs (DI設定)
│   ├── Spawner.cs
│   ├── Tetromino.cs
│   ├── GridBorder.cs
│   └── CameraAutoFit.cs
└── UI/              ← UI関連
    ├── TouchController.cs
    └── ExitConfirmDialog.cs
```

---

## 4. クラス詳細

### 4.1 Model層

#### GameModel.cs
ゲーム全体の状態を管理するモデルクラス

| メソッド | 説明 |
|---|---|
| `GetCell(x, y)` | 指定座標のグリッドセルを取得 |
| `SetCell(x, y, block)` | 指定座標にブロックを配置 |
| `IsValidCell(x, y)` | 座標が有効範囲内かつ空いているかチェック |
| `IsRowFull(y)` | 指定行が完全に埋まっているかチェック |
| `ClearRow(y)` | 指定行をクリア |
| `ShiftRowsDown(y)` | 指定行より上のブロックを1行下に移動 |
| `AddLinesCleared(count)` | ライン消去時のスコア加算 |
| `TriggerGameOver()` | ゲームオーバーを発生 |
| `Reset()` | ゲームをリセット |

**ReactiveProperty（R3）**
- `Score` - 現在のスコア
- `LinesCleared` - 消去ライン数
- `IsGameOver` - ゲームオーバー状態

### 4.2 View層

#### IGameView.cs（インターフェース）
ViewをPresenterから抽象化するためのインターフェース

| メソッド/プロパティ | 説明 |
|---|---|
| `UpdateScore(score)` | スコア表示を更新 |
| `UpdateLines(lines)` | 消去ライン数表示を更新 |
| `ShowGameOverPanel(show)` | ゲームオーバーパネルの表示切替 |
| `OnRetryClicked` | リトライボタンクリックイベント |

#### IInputView.cs（インターフェース）
入力イベントの抽象化

| プロパティ | 説明 |
|---|---|
| `OnMoveLeft` | 左移動イベント |
| `OnMoveRight` | 右移動イベント |
| `OnRotate` | 回転イベント |
| `OnHardDrop` | ハードドロップイベント |
| `OnSoftDrop` | ソフトドロップ状態 |

### 4.3 Presenter層

#### GamePresenter.cs
ModelとViewを仲介し、ゲーム全体を制御

| メソッド | 説明 |
|---|---|
| `Start()` | 初期化、購読設定 |
| `HandleRetry()` | リトライ処理 |
| `AddTetrominoToGrid(blocks)` | テトロミノをグリッドに追加 |
| `CheckAndClearLines()` | ライン消去チェックと実行 |
| `IsValidPosition(transform, blocks)` | 位置の有効性チェック |
| `CheckGameOver()` | ゲームオーバー判定 |

### 4.4 Core層

#### Tetromino.cs
テトロミノの動作を管理

| メソッド | 説明 |
|---|---|
| `Move(direction)` | テトロミノを移動 |
| `Rotate()` | テトロミノを回転 |
| `HardDrop()` | 即座に落下 |
| `LockTetromino()` | テトロミノを固定 |

#### Spawner.cs
テトロミノの生成を管理

#### GridBorder.cs
グリッド境界線の描画

#### CameraAutoFit.cs
カメラの自動調整

#### GameLifetimeScope.cs
VContainerによる依存性注入の設定

### 4.5 UI層

#### TouchController.cs
モバイル用タッチ入力の処理

#### ExitConfirmDialog.cs
終了確認ダイアログ

---

## 5. ゲーム仕様

### 5.1 グリッドサイズ
- 幅: 10マス
- 高さ: 20マス

### 5.2 テトロミノ種類（7種）
| 名称 | 形状 |
|---|---|
| I | ████ |
| O | ██<br>██ |
| T | █<br>███ |
| S | ░██<br>██░ |
| Z | ██░<br>░██ |
| J | █░░<br>███ |
| L | ░░█<br>███ |

### 5.3 スコアシステム
| 消去ライン数 | 得点 |
|---|---|
| 1ライン | 100点 |
| 2ライン（ダブル） | 300点 |
| 3ライン（トリプル） | 500点 |
| 4ライン（テトリス） | 800点 |

### 5.4 ゲームオーバー条件
- 新しいテトロミノがスポーン位置に配置できない場合

---

## 6. 操作方法

### PC（キーボード）
| キー | 動作 |
|---|---|
| ← / → | 左右移動 |
| ↑ | 回転 |
| ↓ | ソフトドロップ |
| Space | ハードドロップ |
| Escape | 終了確認 |

### モバイル
画面上のボタンによる操作

---

## 7. 技術スタック

| ライブラリ | バージョン | 用途 |
|---|---|---|
| **Unity** | 6000.3+ | ゲームエンジン |
| **VContainer** | - | 依存性注入（DI） |
| **R3** | - | Reactive Extensions |
| **UniTask** | - | 非同期処理 |
| **Input System** | - | 入力処理 |

---

## 8. データフロー

```
ユーザー入力
    │
    ▼
[View] ボタンクリック → イベント発火
    │
    ▼
[Presenter] イベント購読 → Modelを更新
    │
    ▼
[Model] 状態変更 → ReactivePropertyで通知
    │
    ▼
[Presenter] 変化を検知 → Viewを更新
    │
    ▼
[View] 表示更新
```

---

## 9. セットアップ方法

詳細なセットアップ手順は [SetupGuide.md](Assets/Scripts/SetupGuide.md) を参照してください。




