using System;
using System.Collections.Generic;
using System.Linq;

namespace Stackdose.UI.Core.Models
{
    /// <summary>
    /// Recipe 資料模型
    /// 代表完整的製程配方
    /// </summary>
    public class Recipe
    {
        /// <summary>
        /// Recipe 唯一識別碼
        /// </summary>
        public string RecipeId { get; set; } = string.Empty;

        /// <summary>
        /// Recipe 名稱
        /// </summary>
        public string RecipeName { get; set; } = string.Empty;

        /// <summary>
        /// Recipe 版本號
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 建立日期
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 最後修改日期
        /// </summary>
        public DateTime? LastModifiedDate { get; set; }

        /// <summary>
        /// 建立者
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// 最後修改者
        /// </summary>
        public string? LastModifiedBy { get; set; }

        /// <summary>
        /// Recipe 描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 產品型號或批次
        /// </summary>
        public string? ProductCode { get; set; }

        /// <summary>
        /// Recipe 項目集合
        /// </summary>
        public List<RecipeItem> Items { get; set; } = new List<RecipeItem>();

        /// <summary>
        /// 是否為預設 Recipe
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// Recipe 狀態 (Active, Draft, Archived)
        /// </summary>
        public string Status { get; set; } = "Active";

        /// <summary>
        /// 取得指定名稱的參數
        /// </summary>
        public RecipeItem? GetItem(string name)
        {
            return Items.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 取得指定位址的參數
        /// </summary>
        public RecipeItem? GetItemByAddress(string address)
        {
            return Items.FirstOrDefault(x => x.Address.Equals(address, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 驗證所有參數值是否在有效範圍內
        /// </summary>
        public (bool isValid, List<string> errors) Validate()
        {
            var errors = new List<string>();

            foreach (var item in Items.Where(x => x.IsEnabled))
            {
                if (string.IsNullOrEmpty(item.Name))
                {
                    errors.Add($"項目缺少名稱: {item.Address}");
                    continue;
                }

                if (string.IsNullOrEmpty(item.Address))
                {
                    errors.Add($"項目 '{item.Name}' 缺少 PLC 位址");
                    continue;
                }

                // 驗證數值範圍
                if (double.TryParse(item.Value, out double value))
                {
                    if (!item.IsValueInRange(value))
                    {
                        errors.Add($"項目 '{item.Name}' 值 {value} 超出範圍 [{item.MinValue}~{item.MaxValue}]");
                    }
                }
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// 取得所有啟用的項目數量
        /// </summary>
        public int EnabledItemCount => Items.Count(x => x.IsEnabled);

        public override string ToString()
        {
            return $"Recipe: {RecipeName} v{Version} ({EnabledItemCount} items)";
        }
    }
}
