

using Prometheus;

namespace Services.BasketService.Infrastructure.Monitoring
{
    public static class RedisMetrics
    {
        public static readonly Counter RedisHitCounter = Metrics
            .CreateCounter("basket_redis_hit_total", "Total Redis cache hits for basket");

        public static readonly Counter RedisMissCounter = Metrics
            .CreateCounter("basket_redis_miss_total", "Total Redis cache misses for basket");
    }
}
