using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Stackdose.Tools.DllValidator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("   Stackdose PrintHead DLL Validator v1.0");
            Console.WriteLine("==================================================");
            
            string targetDll = "FeiyangWrapper.dll";
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string dllPath = Path.Combine(baseDir, targetDll);

            Console.WriteLine($"[1] 檢查執行目錄: {baseDir}");
            
            // 1. 檢查檔案是否存在
            if (!File.Exists(dllPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[-] 錯誤: 找不到 {targetDll}！");
                Console.WriteLine($"    請確認 ..\\..\\..\\Sdk\\ 目錄下是否有對應檔案。");
                Console.ResetColor();
                return;
            }
            Console.WriteLine($"[+] 檔案存在: {dllPath}");

            // 2. 檢查 SDK 依賴項 (通常是同目錄下的其他 DLL)
            var otherDlls = Directory.GetFiles(baseDir, "*.dll");
            Console.WriteLine($"[2] 偵測到同目錄 DLL 數量: {otherDlls.Length}");

            // 3. 嘗試動態載入
            Console.WriteLine($"[3] 正在嘗試載入 {targetDll} 並解析依賴鏈...");
            try 
            {
                // 使用 NativeLibrary.Load 會觸發 Windows 的 DLL 載入邏輯
                // 如果缺少 C++ Runtime 或 SDK 的 sub-DLL，這裡會拋出異常
                IntPtr handle = NativeLibrary.Load(dllPath);
                
                if (handle != IntPtr.Zero)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[SUCCESS] DLL 載入成功！");
                    Console.WriteLine($"          Handle: 0x{handle.ToString("X")}");
                    Console.ResetColor();
                    
                    // 嘗試尋找基礎符號 (假定有一個 GetVersion 或類似函式，這裡僅測試 LoadSymbol 是否報錯)
                    // Note: 即使找不到符號，只要 Load 成功，就代表依賴鏈沒問題
                    NativeLibrary.Free(handle);
                }
            }
            catch (DllNotFoundException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FAILED] 載入失敗: 找不到相依的 DLL 模組。");
                Console.WriteLine($"         訊息: {ex.Message}");
                Console.WriteLine("         建議: 檢查是否安裝了 VC++ Redistributable，");
                Console.WriteLine("               或 SDK 目錄下的子 DLL 是否有遺漏。");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FAILED] 發生未知錯誤: {ex.GetType().Name}");
                Console.WriteLine($"         訊息: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\n按任意鍵結束驗證...");
        }
    }
}
