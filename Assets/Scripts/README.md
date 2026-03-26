GameCore 負責整體遊戲運行的邏輯，他會管理目前是哪一段劇情還是在等使用者互動，並且呼叫對應的程式碼。
目前播放故事以及等待使用者互動的程式碼都還沒實現，註解在 114~116，以及 137~138 行，未來實現後從那裡呼叫。
希望每一段故事、每一段互動都獨立一個檔案 or 一個資料夾，否則 GameCore 可能會上千行。
使用 NotifyInteractionFinished(), NotifyStoryFinished() 告訴 GameCoe 應該要進行下一階段了。
使用 GameCore.OnInteractionCompleted += Handler 或其他類似的程式碼(26~31行)可以在階段發生變化時自動呼叫 Handler

BirdManager 負責管理鳥群移動
將所有鳥類放在一個 EmptyObject 內，並且掛在 Birds Root 內，代表一個要被移動的鳥群
所有的鳥必須要掛載 BirdAgent
建立一個 EmptyObject，裡面放入一堆 EmptyObject，並且設定好他們的位置，掛在 Trajectory Root 內，鳥群會沿著這些 EmptyObject 移動
還有一些參數，Wayponint Reach Distance 的意思是鳥群中心離 waypoint 中心多近時，會將目標轉移到下一個 waypoint
Seperation Radius 控制鳥彼此會保持多大的距離

BirdAgent 會接收 BirdManager 傳來的移動指令
未來需要新增控制動畫的功能，可能視情況新增一些跟地面相關的動作