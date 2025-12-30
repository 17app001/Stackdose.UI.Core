namespace Stackdose.UI.Core.Models
{
    /// <summary>
    /// 存取權限等級 (符合 FDA 21 CFR Part 11)
    /// </summary>
    public enum AccessLevel
    {
        /// <summary>
        /// 等級 0：未登入 (Guest)
        /// </summary>
        Guest = 0,

        /// <summary>
        /// 等級 1：操作員 (Operator)
        /// 權限：日常生產操作（開機/關機/清潔/啟動製程）
        /// </summary>
        Operator = 1,

        /// <summary>
        /// 等級 2：指導員 (Instructor)
        /// 權限：Operator + 警報處理 + 查看/匯出日誌
        /// </summary>
        Instructor = 2,

        /// <summary>
        /// 等級 3：主管 (Supervisor)
        /// 權限：Instructor + 管理 Level 1-2 帳號
        /// </summary>
        Supervisor = 3,

        /// <summary>
        /// 等級 4：管理員 (Admin)
        /// 權限：最高權限，可修改系統參數
        /// </summary>
        Admin = 4
    }
}
