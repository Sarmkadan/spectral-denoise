using SpectralDenoise;
using Xunit;

namespace SpectralDenoise.Tests;

public class AudioPipelineTests
{
    private const int SampleRate = 44_100;

    [Fact]
    public void Constructor_WithNullProcessors_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AudioPipeline((IAudioProcessor[])null!));
    }

    [Fact]
    public void Constructor_WithEmptyProcessors_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new AudioPipeline(Array.Empty<IAudioProcessor>()));
    }

    [Fact]
    public void Constructor_WithNullProcessor_ThrowsArgumentException()
    {
        IAudioProcessor[] processors = [new TaggingProcessor(1), null!];

        Assert.ThrowsAny<ArgumentException>(() => new AudioPipeline(processors));
    }

    [Fact]
    public void Process_AppliesProcessorsInDeclaredOrder()
    {
        var pipeline = new AudioPipeline(
            new TaggingProcessor(10),
            new TaggingProcessor(20),
            new TaggingProcessor(30));

        float[] result = pipeline.Process([1, 2], SampleRate);

        Assert.Equal(new float[] { 1, 2, 10, 20, 30 }, result);
    }

    [Fact]
    public void Process_WithSingleProcessor_BehavesLikeProcessor()
    {
        var processor = new TaggingProcessor(42);
        var pipeline = new AudioPipeline(processor);
        float[] samples = [3, 6, 9];

        float[] expected = processor.Process(samples, SampleRate);
        float[] actual = pipeline.Process(samples, SampleRate);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NamedConstructor_StoresNameWithoutAffectingProcessing()
    {
        const string name = "Tagged pipeline";
        IAudioProcessor[] processors = [new TaggingProcessor(7), new TaggingProcessor(8)];
        var namedPipeline = new AudioPipeline(name, processors);
        var unnamedPipeline = new AudioPipeline(processors);
        float[] samples = [1, 2, 3];

        float[] namedResult = namedPipeline.Process(samples, SampleRate);
        float[] unnamedResult = unnamedPipeline.Process(samples, SampleRate);

        Assert.Equal(name, namedPipeline.Name);
        Assert.Equal(unnamedResult, namedResult);
    }

    private sealed class TaggingProcessor(float tag) : IAudioProcessor
    {
        public float[] Process(float[] samples, int sampleRate)
        {
            var tagged = new float[samples.Length + 1];
            samples.CopyTo(tagged, 0);
            tagged[^1] = tag;
            return tagged;
        }

        public void Reset()
        {
        }
    }
}
