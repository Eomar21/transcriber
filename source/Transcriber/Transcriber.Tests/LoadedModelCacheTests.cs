public class LoadedModelCacheTests
{
    private sealed class Model(string name) : IDisposable
    {
        public string Name { get; } = name;
        public bool Released { get; private set; }
        public void Dispose() => Released = true;
    }

    private readonly List<Model> _loaded = [];

    private LoadedModelCache<string, Model> Cache(TimeSpan keepFor) => new(name =>
    {
        var model = new Model(name);
        _loaded.Add(model);
        return model;
    }, keepFor);

    private static readonly TimeSpan Long = TimeSpan.FromMinutes(10);

    [Fact]
    public void The_same_model_is_loaded_one_time_for_two_runs()
    {
        using var cache = Cache(Long);

        var first = cache.Take("small");
        cache.Return();
        var second = cache.Take("small");
        cache.Return();

        Assert.Same(first, second);
        Assert.Single(_loaded);
        Assert.False(first.Released);
    }

    [Fact]
    public void Another_model_releases_the_one_before_it()
    {
        using var cache = Cache(Long);

        var small = cache.Take("small");
        cache.Return();
        var medium = cache.Take("medium");

        Assert.True(small.Released, "two models must not be in memory at the same time");
        Assert.False(medium.Released);
        Assert.Equal("medium", medium.Name);
    }

    [Fact]
    public async Task A_model_that_is_not_used_is_released_after_the_time()
    {
        using var cache = Cache(TimeSpan.FromMilliseconds(50));
        var model = cache.Take("small");
        cache.Return();

        for (var wait = 0; wait < 100 && !model.Released; wait++) await Task.Delay(20);

        Assert.True(model.Released);
        Assert.NotSame(model, cache.Take("small"));
    }

    [Fact]
    public async Task A_model_that_is_in_use_is_not_released_by_the_time()
    {
        using var cache = Cache(TimeSpan.FromMilliseconds(30));
        cache.Take("small");
        cache.Return();
        var model = cache.Take("small"); // a long run starts before the time is over

        await Task.Delay(200);

        Assert.False(model.Released);
    }

    [Fact]
    public void With_no_time_the_model_is_released_at_the_end_of_the_run()
    {
        using var cache = Cache(TimeSpan.Zero);

        var model = cache.Take("small");
        Assert.False(model.Released);
        cache.Return();

        Assert.True(model.Released);
    }

    [Fact]
    public void A_model_cannot_be_taken_two_times_at_once()
    {
        using var cache = Cache(Long);
        cache.Take("small");

        Assert.Throws<InvalidOperationException>(() => cache.Take("small"));
    }

    [Fact]
    public void A_load_that_fails_leaves_the_cache_ready_for_the_next_run()
    {
        var fail = true;
        using var cache = new LoadedModelCache<string, Model>(name => fail ? throw new IOException("no file") : new Model(name), Long);

        Assert.Throws<IOException>(() => cache.Take("small"));
        fail = false;

        Assert.Equal("small", cache.Take("small").Name);
    }

    [Fact]
    public void Closing_the_cache_releases_the_model()
    {
        var cache = Cache(Long);
        var model = cache.Take("small");
        cache.Return();

        cache.Dispose();

        Assert.True(model.Released);
    }
}
