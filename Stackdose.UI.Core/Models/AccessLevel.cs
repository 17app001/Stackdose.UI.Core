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
        /// 權限：基本操作（設備/啟動/暫停/輸入製程批次）
        /// </summary>
        Operator = 1,

        /// <summary>
        /// 等級 2：指導員 (Instructor)
        /// 權限：Operator + 異常處理 + 查看/匯出紀錄
        /// </summary>
        Instructor = 2,

        /// <summary>
        /// 等級 3：主管 (Supervisor)
        /// 權限：Instructor + 取消製程
        /// </summary>
        Supervisor = 3,

        /// <summary>
        /// 等級 4：管理員 (Admin)
        /// 權限：Supervisor + 帳號管理
        /// </summary>
        Admin = 4,

        /// <summary>
        /// 等級 5：超級管理員 (SuperAdmin)
        /// 權限：完全權限 + 製程參數管理 + 工程模式
        /// </summary>
        SuperAdmin = 5
    }
}
