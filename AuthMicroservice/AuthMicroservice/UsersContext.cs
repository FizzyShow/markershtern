﻿using AuthMicroservice.Configs;
using AuthMicroservice.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AuthMicroservice
{
    public class UsersContext
    {
        private readonly IMongoCollection<User> _usersCollection;

        public UsersContext(IOptions<MongoDBSettings> MongoDBSettings)
        {
            var mongoClient = new MongoClient(
                MongoDBSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                MongoDBSettings.Value.DatabaseName);

            _usersCollection = mongoDatabase.GetCollection<User>(
                MongoDBSettings.Value.CollectionName);
        }

        public async Task<List<User>> GetAsync() =>
            await _usersCollection.Find(_ => true).ToListAsync();

        public async Task<User?> GetAsyncByID(string id) =>
            await _usersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<User?> GetAsyncByLogin(string login) =>
    await _usersCollection.Find(x => x.Login == login).FirstOrDefaultAsync();

        public async Task CreateAsync(User newUser) =>
            await _usersCollection.InsertOneAsync(newUser);

        public async Task UpdateAsync(string id, User updatedUser) =>
            await _usersCollection.ReplaceOneAsync(x => x.Id == id, updatedUser);

        public async Task RemoveAsync(string id) =>
            await _usersCollection.DeleteOneAsync(x => x.Id == id);
    }
}
