# XR-project1

## Meta Simulator

### Prework

- 下載 [Meta XR All-in-One SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-all-in-one-sdk-269657?srsltid=AfmBOopcRTyK4Yx3lVShqe20-0Enp_fJ0wSYD0X0lgnDR9f9yOOeXc2W)
- 下載 [Meta XR Simulator](https://developers.meta.com/horizon/documentation/unity/xrsim-getting-started/)

### 選單 Edit > Project Settings

- 找到 XR Plug-in Management 並展開  
  - 確保 OpenXR 已被勾選  
- 找到 OpenXR 並展開  
  - Enabled Interaction Profiles 區塊下加入 Oculus Touch Controller Profile

### 啟動 MetaXRSimulator

- 打開 MetaXRSimulator  
- 將左上角設為 Activate  
- 點擊 Unity 中的 Play 啟動遊戲

### 操縱方式 (Game window in Unity)

Keyboard

- WASD: 前後左右
- Space: 跳躍
- Q/E: 轉動視角

### 注意事項

- Edit -> Project Settings... -> Player -> Other Settings -> Active Input Handling*
  - 改成 Both
- [BuildingBlock] Camera Rig -> OVR Manager (Scrpit) -> Tracking -> Tracking Origin Type
  - Simulation mode 下 play 後可能會跑掉，要改回 eye level
