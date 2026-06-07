using FluentAssertions;
using GroceryStoreManagement.Helpers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Xunit;

namespace GroceryStoreManagement.Tests
{
    public class ExportHelperTests
    {
        private sealed class SampleRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Amount { get; set; }
        }

        [Fact]
        public void ExportToCSV_Should_CreateUtf8File_WithArabicContent()
        {
            var rows = new List<SampleRow>
            {
                new() { Id = 1, Name = "منتج عربي", Amount = 10.5m },
                new() { Id = 2, Name = "Product EN", Amount = 20m }
            };
            string path = Path.Combine(Path.GetTempPath(), $"export_csv_{Path.GetRandomFileName()}.csv");

            try
            {
                bool result = ExportHelper.ExportToCSV(rows, path);

                result.Should().BeTrue();
                File.Exists(path).Should().BeTrue();
                string content = File.ReadAllText(path);
                content.Should().Contain("منتج عربي");
                content.Should().Contain("Name");
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void ExportToCSV_WithHeaderMap_Should_UseArabicHeaders()
        {
            var rows = new List<SampleRow>
            {
                new() { Id = 1, Name = "منتج", Amount = 15m }
            };
            var headerMap = new Dictionary<string, string>
            {
                ["Id"] = "المعرف",
                ["Name"] = "الاسم",
                ["Amount"] = "المبلغ"
            };
            string path = Path.Combine(Path.GetTempPath(), $"export_csv_map_{Path.GetRandomFileName()}.csv");

            try
            {
                bool result = ExportHelper.ExportToCSV(rows, path, headerMap);

                result.Should().BeTrue();
                string? firstLine = File.ReadLines(path).FirstOrDefault();
                firstLine.Should().Be("المعرف,الاسم,المبلغ");
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void ExportToExcel_Should_CreateFile()
        {
            var rows = new List<SampleRow>
            {
                new() { Id = 1, Name = "اختبار", Amount = 100m }
            };
            string path = Path.Combine(Path.GetTempPath(), $"export_xlsx_{Path.GetRandomFileName()}.xlsx");

            try
            {
                bool result = ExportHelper.ExportToExcel(rows, path);

                result.Should().BeTrue();
                File.Exists(path).Should().BeTrue();
                new FileInfo(path).Length.Should().BeGreaterThan(0);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void ExportToExcel_WithHeaderMap_Should_UseArabicHeaders()
        {
            var rows = new List<SampleRow>
            {
                new() { Id = 1, Name = "اختبار", Amount = 100m }
            };
            var headerMap = new Dictionary<string, string>
            {
                ["Id"] = "المعرف",
                ["Name"] = "الاسم",
                ["Amount"] = "المبلغ"
            };
            string path = Path.Combine(Path.GetTempPath(), $"export_xlsx_map_{Path.GetRandomFileName()}.xlsx");

            try
            {
                bool result = ExportHelper.ExportToExcel(rows, path, headerMap);

                result.Should().BeTrue();

                using var archive = ZipFile.OpenRead(path);
                var sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml");
                sharedStringsEntry.Should().NotBeNull();
                using var stream = sharedStringsEntry!.Open();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                string xml = reader.ReadToEnd();
                xml.Should().Contain("المعرف");
                xml.Should().Contain("الاسم");
                xml.Should().Contain("المبلغ");
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void ExportToPdf_Should_CreateReadablePdfFile()
        {
            var rows = new List<SampleRow>
            {
                new() { Id = 1, Name = "تقرير عربي", Amount = 55m },
                new() { Id = 2, Name = "Second Row", Amount = 70m }
            };

            string path = Path.Combine(Path.GetTempPath(), $"export_pdf_{Path.GetRandomFileName()}.pdf");
            var headers = new Dictionary<string, string>
            {
                ["Id"] = "المعرف",
                ["Name"] = "الاسم",
                ["Amount"] = "المبلغ"
            };

            try
            {
                bool result = ExportHelper.ExportToPDF(rows, path, "تقرير الاختبار", headers);

                result.Should().BeTrue();
                File.Exists(path).Should().BeTrue();
                new FileInfo(path).Length.Should().BeGreaterThan(0);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
