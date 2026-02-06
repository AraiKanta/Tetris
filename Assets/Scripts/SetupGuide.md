# Unity テトリス セットアップガイド（MVP + VContainer + R3 + UniTask）

## アーキテクチャ概要

本プロジェクトは**MVP（Model-View-Presenter）設計**を採用しています。

```
┌─────────────────────────────────────────────────────────────┐
│                      VContainer (DI)                         │
│  GameLifetimeScope: Model/View/Presenterの依存関係を管理      │
└─────────────────────────────────────────────────────────────┘
                              │
       ┌──────────────────────┼──────────────────────┐
       ▼                      ▼                      ▼
┌─────────────┐      ┌──────────────┐      ┌─────────────┐
│   Model     │◄────►│  Presenter   │◄────►│    View     │
│  GameModel  │      │GamePresenter │      │  GameView   │
│             │      │              │      │             │
│ ・スコア    │      │ ・仲介役     │      │ ・UI表示    │
│ ・ライン数  │      │ ・ロジック   │      │ ・入力イベント│
│ ・グリッド  │      │ ・購読管理   │      │             │
└─────────────┘      └──────────────┘      └─────────────┘
      R3で通知 ─────────►│◄─────── R3で購読
```

---

## 1. フォルダ構成

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

## 2. 基本設定

### カメラ
1. Main Camera → **CameraAutoFit.cs** をアタッチ
2. 自動的に画面に合わせて調整される

---

## 3. Prefab作成

### ブロックPrefab
1. Sprites > Square を作成
2. Scale: `(0.95, 0.95, 1)`
3. Prefab化して保存

### テトロミノPrefab（7種類）
1. 空のGameObjectを作成
2. **Tetromino.cs** をアタッチ
3. ブロックを4つ子として配置
4. Pivotオブジェクトを作成してアサイン

---

## 4. シーン設定

### 4.1 GameLifetimeScope
1. Create Empty → `GameLifetimeScope`
2. **GameLifetimeScope.cs** をアタッチ
3. 設定:
   - **Tetromino Prefabs**: 7種類
   - **Spawn Position**: `(5, 19, 0)`
   - **Game View**: GameViewコンポーネントをアサイン

### 4.2 Spawner
1. Create Empty → `Spawner`
2. **Spawner.cs** をアタッチ

### 4.3 GridBorder
1. Create Empty → `GridBorder`
2. **GridBorder.cs** をアタッチ

---

## 5. UI設定

### 5.1 Canvas作成
1. GameObject > UI > Canvas
2. Canvas Scaler: Scale With Screen Size

### 5.2 GameView（重要！）
1. Create Empty → `GameView`
2. **GameView.cs** をアタッチ
3. 以下をアサイン:
   - **Score Text**: TextMeshProUGUI
   - **Lines Text**: TextMeshProUGUI
   - **Game Over Panel**: Panel（子にRetryButton）
   - **Retry Button**: Button

### 5.3 TouchController（モバイル用）
1. Create Empty → `TouchController`
2. **TouchController.cs** をアタッチ

### 5.4 ExitConfirmDialog
1. **ExitConfirmDialog.cs** をアタッチ
2. 確認ダイアログのUIをアサイン

---

## 6. 操作方法

### PC
| キー | 動作 |
|------|------|
| ← → | 左右移動 |
| ↑ | 回転 |
| ↓ | ソフトドロップ |
| Space | ハードドロップ |
| Escape | 終了確認 |

### モバイル
画面のボタン操作

---

## 7. MVP各層の責務

### Model（GameModel.cs）
- **データを保持**: スコア、ライン、グリッド状態
- **ロジック**: ライン消去判定、スコア計算
- **通知**: ReactivePropertyで状態変化を通知
- **特徴**: Unityに依存しない純粋なC#クラス

### View（GameView.cs）
- **表示**: スコア、ライン、ゲームオーバー
- **入力発火**: ボタンクリックをイベントで通知
- **特徴**: ロジックを持たない（Passive View）

### Presenter（GamePresenter.cs）
- **仲介**: ModelとViewを接続
- **購読**: Modelの変化を検知してViewを更新
- **制御**: Viewの入力を受けてModelを操作
- **特徴**: MVP全体のオーケストレーター

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
