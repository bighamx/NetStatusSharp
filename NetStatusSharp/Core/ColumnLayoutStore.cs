using System.IO;
using System.Text.Json;
using System.Windows.Controls;

namespace NetStatusSharp.Core;

/// <summary>
/// 记住表格列宽，位于 %APPDATA%\NetStatusSharp\layout.json。
///
/// 保存的是列的 <see cref="DataGridLength"/>（数值 + 单位）而不是实际渲染宽度：
/// 星级列的 Width 在窗口缩放时会重新分配，但其 Value/UnitType 不变，
/// 只有用户真正拖动列宽时单位才会从 Star 变成 Pixel。因此按此保存既不会
/// 把窗口尺寸变化误记成列宽调整，也能在无冗余的列集合变化时安全退化。
/// </summary>
internal static class ColumnLayoutStore
{
    private static readonly string LayoutPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NetStatusSharp",
        "layout.json");

    private sealed class Layout
    {
        public List<ColumnWidth> Columns { get; set; } = new();
    }

    private sealed class ColumnWidth
    {
        public int Unit { get; set; }

        public double Value { get; set; }
    }

    public static void Load(DataGrid grid)
    {
        try
        {
            if (!File.Exists(LayoutPath))
            {
                return;
            }

            var layout = JsonSerializer.Deserialize<Layout>(File.ReadAllText(LayoutPath));
            if (layout?.Columns is null || layout.Columns.Count != grid.Columns.Count)
            {
                // 列集合变了（版本升级等），旧布局按序号已不可靠，直接用默认值。
                return;
            }

            for (var i = 0; i < grid.Columns.Count; i++)
            {
                var saved = layout.Columns[i];
                if (double.IsNaN(saved.Value) || saved.Value <= 0)
                {
                    continue;
                }

                var unit = saved.Unit == (int)DataGridLengthUnitType.Star
                    ? DataGridLengthUnitType.Star
                    : DataGridLengthUnitType.Pixel;

                grid.Columns[i].Width = new DataGridLength(saved.Value, unit);
            }
        }
        catch
        {
            // 布局文件损坏时忽略，退回 XAML 中的默认列宽。
        }
    }

    public static void Save(DataGrid grid)
    {
        try
        {
            var layout = new Layout();
            foreach (var column in grid.Columns)
            {
                layout.Columns.Add(new ColumnWidth
                {
                    Unit = (int)column.Width.UnitType,
                    Value = column.Width.Value,
                });
            }

            var directory = Path.GetDirectoryName(LayoutPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(LayoutPath, JsonSerializer.Serialize(layout));
        }
        catch
        {
            // 保存失败不影响使用。
        }
    }
}
