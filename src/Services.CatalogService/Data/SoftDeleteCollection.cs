using MongoDB.Driver;
using System.Linq.Expressions;
using System.Reflection;

namespace Services.CatalogService.Data
{
    //lớp SoftDeleteCollection<T> bọc quanh IMongoCollection<T> — nó thêm filter IsDeleted == false cho mọi Find / FindAsync
    public class SoftDeleteCollection<T> where T : class
    {
        private readonly IMongoCollection<T> _inner;
        private readonly PropertyInfo? _isDeletedProp;

        public SoftDeleteCollection(IMongoCollection<T> inner)
        {
            _inner = inner;
            _isDeletedProp = typeof(T).GetProperty("IsDeleted");
        }

        private FilterDefinition<T> ApplySoftDeleteFilter(FilterDefinition<T>? filter)
        {
            if (_isDeletedProp == null) return filter ?? Builders<T>.Filter.Empty;

            var builder = Builders<T>.Filter;
            var softFilter = builder.Ne("IsDeleted", true);
            return filter != null ? builder.And(softFilter, filter) : softFilter;
        }

        // Tự động filter IsDeleted == false
        public IFindFluent<T, T> Find(FilterDefinition<T> filter)
        {
            return _inner.Find(ApplySoftDeleteFilter(filter));
        }

        public async Task<List<T>> FindAsync(FilterDefinition<T> filter)
        {
            return await _inner.Find(ApplySoftDeleteFilter(filter)).ToListAsync();
        }

        public async Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate)
        {
            var builder = Builders<T>.Filter;
            var filter = builder.Where(predicate);
            return await _inner.Find(ApplySoftDeleteFilter(filter)).FirstOrDefaultAsync();
        }

        // Cho phép bỏ qua soft-delete filter khi cần
        public IFindFluent<T, T> FindIncludingDeleted(FilterDefinition<T> filter)
        {
            return _inner.Find(filter);
        }

        // Proxy các hàm khác của IMongoCollection
        public Task InsertOneAsync(T entity) => _inner.InsertOneAsync(entity);
        public Task<ReplaceOneResult> ReplaceOneAsync(Expression<Func<T, bool>> predicate, T entity)
            => _inner.ReplaceOneAsync(predicate, entity);
        public Task<UpdateResult> UpdateOneAsync(Expression<Func<T, bool>> predicate, UpdateDefinition<T> update)
            => _inner.UpdateOneAsync(predicate, update);
        public Task<long> CountDocumentsAsync(FilterDefinition<T> filter)
            => _inner.CountDocumentsAsync(ApplySoftDeleteFilter(filter));
        public IMongoCollection<T> Raw => _inner;
    }
}
