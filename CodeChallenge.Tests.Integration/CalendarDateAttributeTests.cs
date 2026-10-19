using CodeChallenge.Models.Requests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeChallenge.Tests.Integration
{
    [TestClass]
    public class CalendarDateAttributeTests
    {
        [TestMethod]
        public void IsValid_AcceptsNullAndPlainDates_AndRejectsTheRest()
        {
            // Arrange
            var attribute = new CalendarDateAttribute();

            // Assert
            Assert.IsTrue(attribute.IsValid(null), "Null is left to [Required] to decide.");
            Assert.IsTrue(attribute.IsValid("2026-01-01"));
            Assert.IsFalse(attribute.IsValid("2026-01-01T00:00:00Z"));
            Assert.IsFalse(attribute.IsValid(""));
            Assert.IsFalse(attribute.IsValid(20260101), "Only text can be a date.");
        }
    }
}
