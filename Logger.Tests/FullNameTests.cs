using Xunit;

namespace Logger.Tests;

public class FullNameTests
{
	[Fact]
	public void CanConstruct_FullName_Record()
	{
		// Arrange & Act
		FullName name = new("Inigo", "Montoya");

		// Assert
		Assert.NotNull(name);
	}
}
