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



0. 加入 GameCore，並且呼叫各個場景
1. 初始場景是 GameStart，呼叫掛在 EggNest 下的 EggNestAnimationController.StartAnimation(callback)，可以開始孵化動畫，動畫結束後會呼叫 callback，建議 callback 應該要放 GameCore 的 NotifyStoryFinished，並且修改 137 行的註解，呼叫下一個 scene。可以參考 TestController。記得刪除 Test 物件!!!
2. 吃完果子之後的場景是 AfterEating，呼叫 SceneManager 下的 FlyingSceneManager.PlayHoveringAnimation() 可以播放盤旋動畫，那堆鳥會在天空盤旋，呼叫 PlayLastAnimation() 那堆鳥會飛向遠方，可參考 TestController2。記得刪除 Test 物件!!!
3. 如果要新增鳥，要放在 Flock 物件內，才會跟著鳥群一起飛。
4. 如果要修改盤旋軌跡，可以修改 FlockWayPoints，鳥會沿著裡面的每個物件的位置循環飛行
5. 如果要修改最後飛行朝向，可以修改 LastWp，概念和 FlockWayPoints 一樣。