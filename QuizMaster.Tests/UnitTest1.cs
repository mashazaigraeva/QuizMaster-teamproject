

public class InfrastructureTests
{
    [Test]
    public void CiCdPipeline_IsWorking_Correctly()
    { 
        int expectedValue = 2;
        int actualValue = 2;

        Assert.That(actualValue, Is.EqualTo(expectedValue));
    }
}
