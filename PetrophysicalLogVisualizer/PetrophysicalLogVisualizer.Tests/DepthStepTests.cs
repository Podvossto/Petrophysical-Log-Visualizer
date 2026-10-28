using PetrophysicalLogVisualizer.App;
using Xunit;

namespace PetrophysicalLogVisualizer.Tests
{
    /// <summary>The depth axis gets about one label per 28 px of plot area, so a small window is as fine as the big PNG.</summary>
    public class DepthStepTests
    {
        [Theory]
        [InlineData(10300, 360, 500)]    // 14,000 ft-class well in a small window: labels every 500 ft, not 1000
        [InlineData(10300, 1380, 200)]   // the 1200 x 1500 PNG
        [InlineData(100, 360, 5)]        // a short well
        [InlineData(1, 360, 0.05)]       // a one-row well (range forced to 1): still a positive step
        public void Step_is_1_2_or_5_times_a_power_of_ten(double range, int plotHeight, double expected)
        {
            Assert.Equal(expected, WellPlotRenderer.DepthStep(range, plotHeight), 6);
        }

        [Fact]
        public void A_tiny_plot_area_does_not_give_zero_or_negative()
        {
            Assert.True(WellPlotRenderer.DepthStep(5000, 0) > 0);
        }
    }
}
