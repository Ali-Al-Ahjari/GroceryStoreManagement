using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace GroceryStoreManagement.Helpers
{
    /// <summary>
    /// مساعد تصدير PDF باستخدام QuestPDF.
    /// </summary>
    public static class PdfExportHelper
    {
        private const string ArabicFontAlias = "GsmArabicFont";
        private static readonly Lock _fontLock = new();
        private static volatile bool _fontRegistered;

        static PdfExportHelper()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static bool ExportCollectionToPdf<T>(
            IEnumerable<T> data,
            string filePath,
            string reportTitle,
            IReadOnlyDictionary<string, string> displayNameMap = null)
        {
            try
            {
                var rows = data?.ToList() ?? [];
                var properties = ResolveProperties<T>(rows, displayNameMap);
                string fontFamily = EnsureArabicFontRegistered();

                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? AppDomain.CurrentDomain.BaseDirectory);

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(25);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontFamily(fontFamily).FontSize(10));

                        page.Header()
                            .ContentFromRightToLeft()
                            .Column(col =>
                            {
                                col.Item().AlignCenter().Text(reportTitle).FontSize(16).SemiBold();
                                col.Item().AlignCenter().Text($"تاريخ التصدير: {DateTime.Now:yyyy/MM/dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                                col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            });

                        page.Content()
                            .PaddingVertical(10)
                            .ContentFromRightToLeft()
                            .Element(x => BuildTable(x, rows, properties));

                        page.Footer()
                            .ContentFromRightToLeft()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("صفحة ");
                                x.CurrentPageNumber();
                                x.Span(" من ");
                                x.TotalPages();
                            });
                    });
                }).GeneratePdf(filePath);

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"فشل تصدير PDF: {reportTitle}");
                return false;
            }
        }

        public static bool ExportTextToPdf(string title, string content, string filePath)
        {
            try
            {
                string fontFamily = EnsureArabicFontRegistered();
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? AppDomain.CurrentDomain.BaseDirectory);

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(25);
                        page.DefaultTextStyle(x => x.FontFamily(fontFamily).FontSize(11));

                        page.Header().ContentFromRightToLeft().AlignCenter().Text(title).FontSize(16).SemiBold();
                        page.Content().ContentFromRightToLeft().PaddingTop(15).Text(content ?? string.Empty);
                    });
                }).GeneratePdf(filePath);

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل تصدير نص إلى PDF");
                return false;
            }
        }

        private static List<ColumnDescriptor> ResolveProperties<T>(
            List<T> rows,
            IReadOnlyDictionary<string, string> displayNameMap)
        {
            Type modelType = typeof(T);

            // في حالة النوع Object نحاول قراءة النوع الحقيقي من أول صف.
            if (modelType == typeof(object) && rows.Count > 0 && rows[0] != null)
            {
                modelType = rows[0].GetType();
            }

            var rawProperties = modelType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .ToList();

            if (displayNameMap == null || displayNameMap.Count == 0)
            {
                return [.. rawProperties.Select(p => new ColumnDescriptor(p, p.Name))];
            }

            var selected = new List<ColumnDescriptor>();
            foreach (var pair in displayNameMap)
            {
                var prop = rawProperties.FirstOrDefault(p => p.Name.Equals(pair.Key, StringComparison.OrdinalIgnoreCase));
                if (prop != null)
                {
                    selected.Add(new ColumnDescriptor(prop, pair.Value));
                }
            }

            // إذا لم تتطابق الخريطة، نرجع جميع الخصائص كـ fallback.
            return selected.Count > 0
                ? selected
                : [.. rawProperties.Select(p => new ColumnDescriptor(p, p.Name))];
        }

        private static void BuildTable<T>(IContainer container, List<T> rows, List<ColumnDescriptor> columns)
        {
            if (columns.Count == 0)
            {
                container.AlignCenter().Text("لا توجد أعمدة متاحة للتصدير.");
                return;
            }

            container.Table(table =>
            {
                table.ColumnsDefinition(definition =>
                {
                    foreach (var _ in columns)
                    {
                        definition.RelativeColumn();
                    }
                });

                table.Header(header =>
                {
                    foreach (var column in columns)
                    {
                        header.Cell().Element(CellHeaderStyle).AlignRight().Text(column.DisplayName);
                    }
                });

                if (rows.Count == 0)
                {
                    table.Cell().ColumnSpan((uint)columns.Count).Element(CellBodyStyle).AlignCenter().Text("لا توجد بيانات.");
                    return;
                }

                foreach (var row in rows)
                {
                    foreach (var column in columns)
                    {
                        object value = row == null ? null : column.Property.GetValue(row);
                        table.Cell().Element(CellBodyStyle).AlignRight().Text(FormatValue(value));
                    }
                }
            });
        }

        private static IContainer CellHeaderStyle(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten3)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(6);
        }

        private static IContainer CellBodyStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten3)
                .Padding(5);
        }

        private static string FormatValue(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value switch
            {
                DateTime date => date.ToString("yyyy/MM/dd HH:mm"),
                DateTimeOffset dateOffset => dateOffset.ToString("yyyy/MM/dd HH:mm"),
                decimal number => number.ToDisplayNumber(),
                double number => number.ToDisplayNumber(),
                float number => number.ToDisplayNumber(),
                _ => value.ToString()
            };
        }

        private static string EnsureArabicFontRegistered()
        {
            if (_fontRegistered)
            {
                return ArabicFontAlias;
            }

            lock (_fontLock)
            {
                if (_fontRegistered)
                {
                    return ArabicFontAlias;
                }

                try
                {
                    foreach (var path in GetFontCandidatePaths())
                    {
                        if (!File.Exists(path))
                        {
                            continue;
                        }

                        using var stream = File.OpenRead(path);
                        QuestPDF.Drawing.FontManager.RegisterFontWithCustomName(ArabicFontAlias, stream);
                        _fontRegistered = true;
                        Logger.LogInfo($"تم تحميل خط PDF العربي: {path}");
                        return ArabicFontAlias;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"تعذر تسجيل الخط العربي المضمّن في PDF. سيتم استخدام Arial. التفاصيل: {ex.Message}");
                }
            }

            return "Arial";
        }

        private static IEnumerable<string> GetFontCandidatePaths()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            yield return Path.Combine(baseDir, "Assets", "Fonts", "NotoNaskhArabic-Regular.ttf");
            yield return Path.Combine(baseDir, "Assets", "Fonts", "NotoSansArabic-Regular.ttf");
            yield return Path.Combine(baseDir, "Fonts", "NotoNaskhArabic-Regular.ttf");
            yield return Path.Combine(baseDir, "Fonts", "NotoSansArabic-Regular.ttf");

            // fallback لنظام ويندوز إذا تعذر تحميل الخط المضمّن.
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts", "arial.ttf");
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts", "segoeui.ttf");
        }

        private sealed record ColumnDescriptor(PropertyInfo Property, string DisplayName);
    }
}
