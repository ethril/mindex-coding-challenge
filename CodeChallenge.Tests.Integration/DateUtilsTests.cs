using System;
using CodeChallenge.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeChallenge.Tests.Integration
{
    [TestClass]
    public class DateUtilsTests
    {
        [TestMethod]
        public void TryParseCalendarDate_ReadsAPlainDateAsMidnightWithNoKind()
        {
            // Execute
            var parsed = DateUtils.TryParseCalendarDate("2026-01-01", out var date);

            // Assert
            Assert.IsTrue(parsed);
            Assert.AreEqual(new DateTime(2026, 1, 1), date);
            Assert.AreEqual(DateTimeKind.Unspecified, date.Kind);
        }

        [TestMethod]
        public void TryParseCalendarDate_RejectsAnythingElse()
        {
            // Arrange
            var notCalendarDays = new[]
            {
                null,
                "",
                " 2026-01-01",
                "2026-01-01 ",
                "2026-1-1",
                "01/01/2026",
                "20260101",
                "2026-02-30",
                "2026-01-01T00:00:00",
                "2026-01-01T00:00:00Z",
                "2026-01-01T00:00:00+09:00",
            };

            foreach (var notCalendarDay in notCalendarDays)
            {
                // Execute
                var parsed = DateUtils.TryParseCalendarDate(notCalendarDay, out var date);

                // Assert
                Assert.IsFalse(parsed, $"'{notCalendarDay}' should be rejected.");
                Assert.AreEqual(default(DateTime), date);
            }
        }

        [TestMethod]
        public void ParseCalendarDate_ReadsAPlainDate_AndThrowsOnAnythingElse()
        {
            // Execute
            var date = DateUtils.ParseCalendarDate("2026-01-01");

            // Assert
            Assert.AreEqual(new DateTime(2026, 1, 1), date);
            Assert.AreEqual(DateTimeKind.Unspecified, date.Kind);
            Assert.ThrowsException<FormatException>(() => DateUtils.ParseCalendarDate("2026-01-01T00:00:00Z"));
        }

        [TestMethod]
        public void FormatCalendarDate_WritesTheDayOnly()
        {
            // Execute
            var text = DateUtils.FormatCalendarDate(new DateTime(2026, 1, 1, 10, 30, 0, DateTimeKind.Utc));

            // Assert
            Assert.AreEqual("2026-01-01", text);
        }
    }
}
