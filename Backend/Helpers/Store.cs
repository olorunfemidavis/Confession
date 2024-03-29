﻿using MongoDB.Driver;
using Newtonsoft.Json;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Backend.Helpers
{
    public class Store
    {
        private static readonly ChatHub hubcontext = new ChatHub();

        private static readonly DataContext context = new DataContext();
        private static readonly DataContextLite contextLite = new DataContextLite();

        public static class UserClass
        {
            internal static UserData FetchUser(string userKey, string appcenter)
            {
                return contextLite.UserData.FindOne(d => d.Key.Contains(userKey) || d.AppCenterID == appcenter);
            }

            internal static void DropUsers()
            {
                List<UserData> allUsers = contextLite.UserData.Find(d => d.AppCenterID == "993285bf-d846-4cac-920b-a5fc92ed6e50").ToList();
                UserData selected = allUsers.FirstOrDefault();
                for (int i = 1; i < allUsers.Count(); i++)
                {
                    selected.Key.AddRange(allUsers[i].Key);
                }

                allUsers.RemoveAt(0);
                foreach (UserData dt in allUsers)
                {
                    contextLite.UserData.Delete(dt.Id);
                }

                contextLite.UserData.Update(selected);

            }

            internal static void AddUser(UserData data)
            {
                if (contextLite.UserData.Exists(d => d.Key.Contains(data.Key[0]) || d.AppCenterID == data.AppCenterID))
                {
                    UpdateUser(data);
                }
                else
                {
                    contextLite.UserData.Insert(data);
                }

            }

            internal static void UpdateUser(UserData data)
            {
                if (contextLite.UserData.Exists(d => d.Key.Contains(data.Key[0]) || d.AppCenterID == data.AppCenterID))
                {
                    UserData user = contextLite.UserData.FindOne(d => d.Key.Contains(data.Key[0]) || d.AppCenterID == data.AppCenterID);
                    user.Biometry = data.Biometry;
                    user.AppCenterID = data.AppCenterID;
                    user.ChatRoomNotification = data.ChatRoomNotification;
                    user.CommentNotification = data.CommentNotification;
                    contextLite.UserData.Update(data);
                }
                else
                {
                    AddUser(data);
                }
            }

            internal static List<UserData> FetchAll()
            {
                return contextLite.UserData.FindAll().ToList();
            }

            internal static UserData FetchByConfessGuid(string confessGuid)
            {
                //fetch confess by guid first
                string confessOwnerID = ConfessClass.FetchOneConfessByGuid(confessGuid).Owner_Guid;
                return UserClass.FetchUserByID(confessOwnerID);

            }

            public static UserData FetchUserByID(string confessOwnerID)
            {
                return contextLite.UserData.FindOne(d => d.Key[0] == confessOwnerID);
            }

            internal static void RegisterOrUpdate(UserData data)
            {
                //fetch the current user. 
                UserData user_data = FetchUser(data.Key[0], data.AppCenterID);
                //A first timer would return null
                if (user_data == null || string.IsNullOrEmpty(user_data.AppCenterID) || user_data.Logger == null || user_data.Key == null)
                {
                    //means a new user; 
                    //Add user to the db
                    user_data = new UserData
                    {
                        AppCenterID = data.AppCenterID,
                        Biometry = false,
                        ChatRoomNotification = true,
                        CommentNotification = true,
                        Logger = data.Logger
                    };
                    //specially treat keys to prevent loss. 
                    if (user_data.Key == null || user_data.Key.Count == 0)
                    {
                        user_data.Key = data.Key;
                    }
                    else
                    {
                        if (!user_data.Key.Contains(data.Key[0]))
                        {
                            user_data.Key.Insert(0, data.Key[0]);
                        }
                    }

                    AddUser(user_data);
                }
                else
                {
                    //An existing user would have all data complete but things might have changed
                    //Note that anything that change like key and token would have been re-created
                    //appcenter id too can change. 
                }
            }
        }

        public static class ChatClass
        {
            internal static List<ChatRoomLoader> FetchRooms(string userKey)
            {
                IEnumerable<ChatRoom> result = contextLite.ChatRoom.FindAll();
                return ChatClass.ChatRoomLoader(result, userKey);
            }
            /// <summary>
            /// Receive string, convert to Chat, Save to db, Convert to Chatloader, return as string.
            /// </summary>
            /// <param name="incoming"></param>
            /// <returns></returns>
            internal static string ProcessMessage(Chat chat)
            {
                //send notification
                try
                {
                    LiteDB.BsonValue newID = contextLite.Chat.Insert(chat);

                    Push.SendChatNotification(chat);
                    //ChatLoader loader = ChatLoader(chat);
                    Chat returnChat = contextLite.Chat.FindById(newID);
                    return JsonConvert.SerializeObject(returnChat);
                }
                catch (Exception ex)
                {
                    System.Threading.Tasks.Task forget_error = hubcontext.Error(ex);
                    return JsonConvert.SerializeObject(new Chat() { });
                }

            }

            private static List<ChatRoomLoader> ChatRoomLoader(IEnumerable<ChatRoom> result, string userKey)
            {
                List<ChatRoomLoader> chatRoomLoaders = new List<ChatRoomLoader>();
                IEnumerable<Chat> chatList = contextLite.Chat.FindAll();
                foreach (ChatRoom room in result)
                {
                    chatRoomLoaders.Add(new ChatRoomLoader()
                    {
                        Id = room.Id,
                        Title = room.Title,
                        MembersCount = room.Members.Count().ToString(),
                        IamMember = room.Members.Contains(userKey),
                        ChatsCount = chatList.Count(d => d.Room_ID == room.Id).ToString()
                    });
                }

                return chatRoomLoaders;
            }

            internal static void LeaveRoom(string userKey, string id)
            {
                ChatRoom room = contextLite.ChatRoom.FindOne(d => d.Id == id);
                if (room.Members.Contains(userKey))
                {
                    room.Members.Remove(userKey);
                }

                contextLite.ChatRoom.Update(room);
            }

            internal static List<ChatLoader> FetchChatByRooms(string userKey, string roomID)
            {
                IEnumerable<Chat> result = contextLite.Chat.Find(f => f.Room_ID == roomID);
                return ChatClass.ChatLoader(result, userKey);
            }
            //fix before uncommenting,
            //private static ChatLoader ChatLoader(Chat chat)
            //{
            //    ChatLoader chatLoader = new ChatLoader()
            //    {
            //        IsMine = false,
            //        Body = chat.Body,
            //        Date = chat.Date,
            //        Room_ID = chat.Room_ID,
            //        SenderName = chat.SenderName,
            //        ChatId = chat.Id,
            //        Quote = new QuoteLoader
            //        {
            //            Body = chat.Quote.Body,
            //            ImageUrl = chat.Quote.ImageUrl,
            //            IsImageAvailable = chat.Quote.IsImageAvailable,
            //            OwnerKey = chat.Quote.OwnerKey,
            //            OwnerName = chat.Quote.OwnerName
            //        },
            //        QuotedChatAvailable = chat.QuotedChatAvailable,
            //        ImageUrl = chat.ImageUrl,
            //        IsImageAvailable = chat.IsImageAvailable,
            //        SenderKey = chat.SenderKey
            //    };

            //    return chatLoader;
            //}


            private static List<ChatLoader> ChatLoader(IEnumerable<Chat> result, string userKey)
            {
                List<ChatLoader> chatLoaders = new List<ChatLoader>();
                foreach (Chat chat in result)
                {
                    ChatLoader new_loader = new ChatLoader()
                    {
                        IsMine = chat.SenderKey.Equals(userKey),
                        Body = chat.Body,
                        Date = chat.Date,
                        Room_ID = chat.Room_ID,
                        SenderName = chat.SenderName,
                        ChatId = chat.Id,
                        QuotedChatAvailable = chat.QuotedChatAvailable,
                        ImageUrl = chat.ImageUrl,
                        IsImageAvailable = chat.IsImageAvailable,
                        SenderKey = chat.SenderKey
                    };
                    if (chat.QuotedChatAvailable & chat.Quote != null)
                    {
                        new_loader.Quote = new QuoteLoader
                        {
                            Body = chat.Quote.Body,
                            ImageUrl = chat.Quote.ImageUrl,
                            IsImageAvailable = chat.Quote.IsImageAvailable,
                            OwnerKey = chat.Quote.OwnerKey,
                            OwnerName = chat.Quote.OwnerName,
                            IsMine = chat.Quote.OwnerName.Equals(userKey)
                        };
                    }

                    chatLoaders.Add(new_loader);
                }

                return chatLoaders;
            }

            internal static List<ChatLoader> AddChat(Chat data, string userKey)
            {
                contextLite.Chat.Insert(data);
                return FetchChatByRooms(userKey, data.Room_ID);
            }

            internal static void DeleteChat(string id)
            {
                contextLite.Chat.Delete(id);
            }

            internal static void LoadRooms(List<ChatRoom> chatRooms)
            {
                IEnumerable<ChatRoom> list = contextLite.ChatRoom.FindAll();
                foreach (ChatRoom room in list)
                {
                    contextLite.ChatRoom.Delete(room.Id);
                }
                contextLite.ChatRoom.InsertBulk(chatRooms);
            }

            internal static void JoinRoom(string userKey, string id)
            {
                ChatRoom room = contextLite.ChatRoom.FindOne(d => d.Id == id);
                if (!room.Members.Contains(userKey))
                {
                    room.Members.Add(userKey);
                }

                contextLite.ChatRoom.Update(room);
            }

            internal static ChatRoom FetchRoomByID(string id)
            {
                return contextLite.ChatRoom.FindOne(f => f.Id == id);
            }

            internal static string GetRoomMemberCount(string roomId)
            {
                return contextLite.ChatRoom.FindOne(d => d.Id == roomId).Members.Count.ToString();
            }
        }

        public static class SettingsClass
        {
            internal static List<string> GetCategories()
            {
                List<string> cats = new List<string>();
                IEnumerable<Confess> confesses = contextLite.Confess.FindAll();
                foreach (Confess dat in confesses)
                {
                    if (!cats.Contains(dat.Category))
                    {
                        cats.Add(dat.Category);
                    }
                }
                return cats;
            }

            internal static void CreateLog(DeviceInfo data)
            {
                contextLite.Logger.Insert(data);
            }
        }
        #region Migrate
        public static class Migrate
        {
            public static void ClearLiteDB()
            {
                List<Confess> allconfess = FetchLite.FetchConfess();
                foreach (Confess data in allconfess)
                {
                    contextLite.Confess.Delete(d => d.Guid == data.Guid);
                }
            }

            internal static void ResetChat()
            {
                //clear the chatdb
                //clear the members in the groups
                //clear the fake user
                List<Chat> chats = contextLite.Chat.FindAll().ToList();
                foreach (Chat chat in chats)
                {
                    contextLite.Chat.Delete(chat.Id);
                }
                List<ChatRoom> allRooms = contextLite.ChatRoom.FindAll().ToList();
                foreach (ChatRoom room in allRooms)
                {
                    room.Members.Clear();
                    contextLite.ChatRoom.Update(room);
                }
                List<LiteDB.BsonDocument> allOldUsers = contextLite.UserRaw.FindAll().ToList();
                foreach (LiteDB.BsonDocument oldUser in allOldUsers)
                {
                    contextLite.UserRaw.Delete(oldUser);
                }
            }

            public static class Create
            {
                public static void CreateConfess(List<Confess> Confess)
                {
                    contextLite.Confess.InsertBulk(Confess);
                }
                public static void CreateConfess(Confess Confess)
                {
                    contextLite.Confess.Insert(Confess);
                }
                public static void CreateSeen(List<Seen> Seen)
                {
                    contextLite.Seen.InsertBulk(Seen);
                }
                public static void CreateLikes(List<Likes> Likes)
                {
                    contextLite.Likes.InsertBulk(Likes);
                }
                public static void CreateComment(List<Comment> Comment)
                {
                    contextLite.Comment.InsertBulk(Comment);
                }
                public static void CreateDislikes(List<Dislikes> Dislikes)
                {
                    contextLite.Dislikes.InsertBulk(Dislikes);
                }
                public static void CreateUser(List<UserData> User)
                {
                    contextLite.UserData.InsertBulk(User);
                }
            }

            public static class Fetch
            {
                public static List<Confess> FetchConfess()
                {
                    FilterDefinitionBuilder<Confess> builder = Builders<Confess>.Filter;
                    FilterDefinition<Confess> empty = builder.Empty;
                    IFindFluent<Confess, Confess> cursor = context.Confess.Find(empty);
                    return cursor.ToList();
                }
                public static List<Seen> FetchSeen()
                {
                    FilterDefinitionBuilder<Seen> builder = Builders<Seen>.Filter;
                    FilterDefinition<Seen> empty = builder.Empty;
                    IFindFluent<Seen, Seen> cursor = context.Seen.Find(empty);
                    return cursor.ToList();
                }
                public static List<Likes> FetchLikes()
                {
                    FilterDefinitionBuilder<Likes> builder = Builders<Likes>.Filter;
                    FilterDefinition<Likes> empty = builder.Empty;
                    IFindFluent<Likes, Likes> cursor = context.Likes.Find(empty);
                    return cursor.ToList();
                }
                public static List<Comment> FetchComment()
                {
                    FilterDefinitionBuilder<Comment> builder = Builders<Comment>.Filter;
                    FilterDefinition<Comment> empty = builder.Empty;
                    IFindFluent<Comment, Comment> cursor = context.Comment.Find(empty);
                    return cursor.ToList();
                }
                public static List<Dislikes> FetchDislikes()
                {
                    FilterDefinitionBuilder<Dislikes> builder = Builders<Dislikes>.Filter;
                    FilterDefinition<Dislikes> empty = builder.Empty;
                    IFindFluent<Dislikes, Dislikes> cursor = context.Dislikes.Find(empty);
                    return cursor.ToList();
                }
                public static List<UserData> FetchUser()
                {
                    FilterDefinitionBuilder<UserData> builder = Builders<UserData>.Filter;
                    FilterDefinition<UserData> empty = builder.Empty;
                    IFindFluent<UserData, UserData> cursor = context.User.Find(empty);
                    return cursor.ToList();
                }
            }
            public static class FetchLite
            {
                public static List<Confess> FetchConfess()
                {
                    return contextLite.Confess.FindAll().ToList();
                }
                public static List<Seen> FetchSeen()
                {
                    return contextLite.Seen.FindAll().ToList();

                }
                public static List<Likes> FetchLikes()
                {
                    return contextLite.Likes.FindAll().ToList();

                }
                public static List<Comment> FetchComment()
                {
                    return contextLite.Comment.FindAll().ToList();
                }
                public static List<Dislikes> FetchDislikes()
                {
                    return contextLite.Dislikes.FindAll().ToList();
                }
                public static List<UserData> FetchUser()
                {
                    return contextLite.UserData.FindAll().ToList();
                }

                public static List<DeviceInfo> FetchLogger()
                {
                    return contextLite.Logger.FindAll().ToList();
                }
            }


            public class MigrateToMongo
            {
                //fetch all documents
                public static Migrator LoadMigration()
                {
                    Migrator migrator = new Migrator();
                    List<object> Lister = new List<object>();
                    Lister.AddRange(FetchLite.FetchConfess());
                    Lister.AddRange(FetchLite.FetchComment());
                    Lister.AddRange(FetchLite.FetchDislikes());
                    Lister.AddRange(FetchLite.FetchLikes());
                    Lister.AddRange(FetchLite.FetchLogger());
                    Lister.AddRange(FetchLite.FetchSeen());
                    Lister.AddRange(FetchLite.FetchUser());
                    migrator.Lister = Lister;
                    try
                    {
                        context.Logger.InsertOne(migrator);

                    }
                    catch (Exception ex)
                    {

                        System.Threading.Tasks.Task forget_error = hubcontext.Error(ex);
                    }
                    return migrator;
                }


            }

        }
        #endregion
        public static class ConfessClass
        {
            public static void DeleteConfess(string guid)
            {
                contextLite.Confess.Delete(d => d.Guid == guid);

                //FilterDefinitionBuilder<Confess> builder = Builders<Confess>.Filter;
                //FilterDefinition<Confess> idFilter = builder.Eq(r => r.Guid, guid);
                //context.Confess.DeleteOne(idFilter);

                //delete all comment for that confession.
                CommentClass.DeleteCommentWithConfessGuid(guid);
            }
            public static void CreateConfess(Confess confess)
            {
                confess.Id = Logical.Setter(confess.Id);
                contextLite.Confess.Insert(confess);
                //context.Confess.InsertOne(confess);
            }
            public static void UpdateConfess(Confess incoming)
            {
                Confess confes = contextLite.Confess.FindOne(d => d.Guid == incoming.Guid);
                confes.Title = incoming.Title;
                confes.Body = incoming.Body;
                confes.Category = incoming.Category;
                contextLite.Confess.Update(confes);

                // FilterDefinition<Confess> filter = Builders<Confess>.Filter.Eq(u => u.Guid, confess.Guid);
                // context.Confess.ReplaceOne(filter, confess);
            }

            public static List<ConfessLoader> FetchConfessByCategory(string category, string key)
            {
                List<Confess> list = contextLite.Confess.Find(d => d.Category == category).ToList();

                //FilterDefinitionBuilder<Confess> builder = Builders<Confess>.Filter;
                //FilterDefinition<Confess> idFilter = builder.Eq(e => e.Category, category);

                //IFindFluent<Confess, Confess> cursor = context.Confess.Find(idFilter);

                // Find All
                return GetConfessLoader(list, key);
            }

            public static List<ConfessLoader> FetchMyConfessions(string key)
            {
                List<Confess> list = contextLite.Confess.Find(d => d.Owner_Guid == key).ToList();

                //FilterDefinitionBuilder<Confess> builder = Builders<Confess>.Filter;
                //FilterDefinition<Confess> idFilter = builder.Eq(e => e.Owner_Guid, key);

                //IFindFluent<Confess, Confess> cursor = context.Confess.Find(idFilter);

                // Find All
                return GetConfessLoader(list, key);
            }
            public static ConfessLoader FetchOneConfessLoader(string guid, string senderKey)
            {
                Confess data = FetchOneConfessByGuid(guid);
                //report seen
                if (data.Owner_Guid != senderKey)
                {
                    SeenClass.Post(guid, senderKey);
                }
                // contextLite.Confess.FindOne(d => d.Guid == guid);

                //FilterDefinitionBuilder<Confess> builder = Builders<Confess>.Filter;
                //FilterDefinition<Confess> idFilter = builder.Eq(e => e.Guid, guid);

                //IFindFluent<Confess, Confess> cursor = context.Confess.Find(idFilter);

                // Find All
                return GetConfessLoader(data, senderKey);
            }

            public static Confess FetchOneConfessByGuid(string guid)
            {
                return contextLite.Confess.FindOne(d => d.Guid == guid);
                //FilterDefinitionBuilder<Confess> builder = Builders<Confess>.Filter;
                //FilterDefinition<Confess> idFilter = builder.Eq(e => e.Guid, guid);

                //IFindFluent<Confess, Confess> cursor = context.Confess.Find(idFilter);

                //// Find One
                //return cursor.FirstOrDefault();
            }

            public static List<ConfessLoader> FetchAllConfess(string key)
            {
                try
                {
                    List<Confess> data = contextLite.Confess.FindAll().ToList();
                    //FilterDefinitionBuilder<Confess> builder = Builders<Confess>.Filter;
                    //FilterDefinition<Confess> empty = builder.Empty;
                    //IFindFluent<Confess, Confess> cursor = context.Confess.Find(empty);
                    return GetConfessLoader(data, key);
                }
                catch (Exception ex)
                {
                    System.Threading.Tasks.Task forget_error = hubcontext.Error(ex);
                    return new List<ConfessLoader>();
                }
            }

            private static List<ConfessLoader> GetConfessLoader(List<Confess> list, string key)
            {
                List<ConfessLoader> loaders = new List<ConfessLoader>();

                foreach (Confess dt in list)
                {
                    ConfessLoader loader = new ConfessLoader
                    {
                        Body = dt.Body,
                        Category = dt.Category,
                        Date = Shared.TimeAgo.Ago(dt.Date),// $"{dt.Date.ToLongDateString()} {dt.Date.ToShortTimeString()}",
                        DateReal = dt.Date,
                        DisLikes = DislikeClass.GetCount(dt.Guid, false),
                        Likes = LikeClass.GetCount(dt.Guid, false),
                        Guid = dt.Guid,
                        Owner_Guid = dt.Owner_Guid,
                        Title = dt.Title,
                        CommentCount = CommentClass.GetCommentCount(dt.Guid),
                        Seen = SeenClass.GetCount(dt.Guid)
                    };
                    //load colors
                    if (LikeClass.CheckExistence(dt.Guid, false, key))
                    {
                        loader.LikeColorString = "#1976D2";
                    }

                    if (DislikeClass.CheckExistence(dt.Guid, false, key))
                    {
                        loader.DislikeColorString = "#1976D2";
                    }

                    loaders.Add(loader);
                    loader = new ConfessLoader();
                }
                loaders = loaders.OrderByDescending(d => d.DateReal).Reverse().ToList();
                return loaders;
            }

            private static ConfessLoader GetConfessLoader(Confess dt, string key)
            {
                ConfessLoader loader = new ConfessLoader
                {
                    Body = dt.Body,
                    Category = dt.Category,
                    Date = Shared.TimeAgo.Ago(dt.Date),//$"{dt.Date.ToLongDateString()} {dt.Date.ToShortTimeString()}",
                    DateReal = dt.Date,
                    DisLikes = DislikeClass.GetCount(dt.Guid, false),
                    Likes = LikeClass.GetCount(dt.Guid, false),
                    Guid = dt.Guid,
                    Owner_Guid = dt.Owner_Guid,
                    Title = dt.Title,
                    CommentCount = CommentClass.GetCommentCount(dt.Guid),
                    Seen = SeenClass.GetCount(dt.Guid)
                };
                //load colors
                if (LikeClass.CheckExistence(dt.Guid, false, key))
                {
                    loader.LikeColorString = "#1976D2";
                }

                if (DislikeClass.CheckExistence(dt.Guid, false, key))
                {
                    loader.DislikeColorString = "#1976D2";
                }

                return loader;
            }

        }

        public static class CommentClass
        {
            private static List<CommentLoader> GetLoader(List<Comment> list, string key)
            {
                List<CommentLoader> loaders = new List<CommentLoader>();
                foreach (Comment dt in list)
                {
                    CommentLoader loader = new CommentLoader
                    {
                        Body = dt.Body,
                        Date = Shared.TimeAgo.Ago(dt.Date),// $"{CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName(dt.Date.DayOfWeek)}, {CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(dt.Date.Month)} {dt.Date.Day}, {dt.Date.Year} {dt.Date.ToShortTimeString()}",
                        DisLikes = DislikeClass.GetCount(dt.Guid, true),
                        DateReal = dt.Date,
                        Likes = LikeClass.GetCount(dt.Guid, true),
                        Guid = dt.Guid,
                        Owner_Guid = dt.Owner_Guid,
                        Quote = dt.Quote,
                        QuotedCommentAvailable = dt.QuotedCommentAvailable,
                        Confess_Guid = dt.Confess_Guid
                    };
                    //load colors
                    if (LikeClass.CheckExistence(dt.Guid, true, key))
                    {
                        loader.LikeColorString = "#1976D2";
                    }

                    if (DislikeClass.CheckExistence(dt.Guid, true, key))
                    {
                        loader.DislikeColorString = "#1976D2";
                    }
                    if (dt.Owner_Guid == key)
                    {
                        loader.DeleteVisibility = true;
                    }

                    loaders.Add(loader);
                    loader = new CommentLoader();
                }
                loaders.OrderByDescending(d => d.DateReal);
                return loaders;
            }

            private static CommentLoader GetLoader(Comment dt, string key)
            {

                CommentLoader loader = new CommentLoader
                {
                    Body = dt.Body,
                    Date = Shared.TimeAgo.Ago(dt.Date),// $"{CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName(dt.Date.DayOfWeek)}, {CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(dt.Date.Month)} {dt.Date.Day}, {dt.Date.Year} {dt.Date.ToShortTimeString()}",
                    DisLikes = DislikeClass.GetCount(dt.Guid, true),
                    DateReal = dt.Date,
                    Likes = LikeClass.GetCount(dt.Guid, true),
                    Guid = dt.Guid,
                    Owner_Guid = dt.Owner_Guid,
                    Quote = dt.Quote,
                    QuotedCommentAvailable = dt.QuotedCommentAvailable,
                    Confess_Guid = dt.Confess_Guid
                };
                //load colors
                if (LikeClass.CheckExistence(dt.Guid, true, key))
                {
                    loader.LikeColorString = "#1976D2";
                }

                if (DislikeClass.CheckExistence(dt.Guid, true, key))
                {
                    loader.DislikeColorString = "#1976D2";
                }
                if (dt.Owner_Guid == key)
                {
                    loader.DeleteVisibility = true;
                }

                return loader;
            }

            public static void DeleteComment(string guid)
            {
                contextLite.Comment.Delete(d => d.Guid == guid);

                //delete others
                contextLite.Likes.Delete(d => d.Comment_Guid == guid);
                contextLite.Dislikes.Delete(d => d.Comment_Guid == guid);

                //FilterDefinitionBuilder<Comment> builder = Builders<Comment>.Filter;
                //FilterDefinition<Comment> idFilter = builder.Eq(r => r.Guid, guid);
                //context.Comment.DeleteOne(idFilter);
            }

            public static void DeleteCommentWithConfessGuid(string confessguid)
            {
                contextLite.Comment.Delete(d => d.Confess_Guid == confessguid);

                //delete others
                contextLite.Likes.Delete(d => d.Confess_Guid == confessguid);
                contextLite.Dislikes.Delete(d => d.Confess_Guid == confessguid);
                contextLite.Seen.Delete(d => d.Confess_Guid == confessguid);

                //FilterDefinitionBuilder<Comment> builder = Builders<Comment>.Filter;
                //FilterDefinition<Comment> idFilter = builder.Eq(r => r.Confess_Guid, confessguid);
                //context.Comment.DeleteMany(idFilter);
            }

            internal static string GetCommentCount(string guid)
            {
                int count = contextLite.Comment.Count(d => d.Confess_Guid == guid);
                return count.ToString();

                //FilterDefinitionBuilder<Comment> builder = Builders<Comment>.Filter;
                //FilterDefinition<Comment> idFilter = builder.Eq(r => r.Confess_Guid, guid);
                //long count = context.Comment.CountDocuments(idFilter);
                //return count.ToString();
            }
            public static void CreateComment(CommentPoster comment)
            {
                comment.Comment.Id = Logical.Setter(comment.Comment.Id);
                contextLite.Comment.Insert(comment.Comment);

                //context.Comment.InsertOne(comment.Comment);

            }
            public static void UpdateComment(Comment comment)
            {
                Comment data = contextLite.Comment.FindOne(d => d.Guid == comment.Guid);
                comment.Id = data.Id;
                contextLite.Comment.Update(comment);

                //FilterDefinition<Comment> filter = Builders<Comment>.Filter.Eq(u => u.Guid, comment.Guid);
                //context.Comment.ReplaceOne(filter, comment);
            }
            public static List<CommentLoader> FetchComment(string guid, string key)
            {
                List<Comment> data = contextLite.Comment.Find(d => d.Confess_Guid == guid).ToList();

                //FilterDefinitionBuilder<Comment> builder = Builders<Comment>.Filter;
                //FilterDefinition<Comment> idFilter = builder.Eq(e => e.Confess_Guid, guid);
                //IFindFluent<Comment, Comment> cursor = context.Comment.Find(idFilter);
                // Find All

                //report seen
                Confess confess = ConfessClass.FetchOneConfessByGuid(guid);
                if (confess.Owner_Guid != key)
                {
                    SeenClass.Post(guid, key);
                }
                return GetLoader(data, key);
            }

            internal static void InsertComment(Comment data)
            {
                data.Id = Logical.Setter(data.Id);
                contextLite.Comment.Insert(data);
            }

            internal static CommentLoader FetchOneCommentLoader(Comment comment)
            {
                Comment query = contextLite.Comment.FindOne(d => d.Guid == comment.Guid);
                return GetLoader(query, comment.Owner_Guid);
            }

            internal static Comment FetchOneCommentByGuid(string commentGuid)
            {
               return contextLite.Comment.FindOne(d => d.Guid == commentGuid);
            }
        }

        public static class LikeClass
        {
            public static bool Post(string guid, bool isComment, string key)
            {
                //check if its disliked
                if (DislikeClass.CheckExistence(guid, isComment, key))
                {
                    return false;
                }
                //Check if there is already a like by the user
                if (CheckExistence(guid, isComment, key))
                {
                    //delete the like
                    Delete(guid, isComment, key);
                    return false;
                }
                //post new like
                Likes like = new Likes
                {
                    Owner_Guid = key,

                };
                if (isComment)
                {
                    like.Comment_Guid = guid;
                }
                else
                {
                    like.Confess_Guid = guid;
                }
                like.Id = Logical.Setter(like.Id);
                contextLite.Likes.Insert(like);
                //context.Likes.InsertOne(like);
                return true;
            }
            public static bool CheckExistence(string guid, bool isComment, string key)
            {
                int count = 0;
                if (isComment)
                {
                    count = contextLite.Likes.Count(d => d.Comment_Guid == guid & d.Owner_Guid == key);
                }
                else
                {
                    count = contextLite.Likes.Count(d => d.Confess_Guid == guid & d.Owner_Guid == key);
                }
                return count > 0;

                //FilterDefinitionBuilder<Likes> builder = Builders<Likes>.Filter;
                //FilterDefinition<Likes> idFilter;
                //if (isComment)
                //{
                //    idFilter = builder.And(builder.Eq(f => f.Comment_Guid, guid), builder.Eq(v => v.Owner_Guid, key));
                //}
                //else
                //{
                //    idFilter = builder.And(builder.Eq(f => f.Confess_Guid, guid), builder.Eq(v => v.Owner_Guid, key));
                //}
                //long cursor = context.Likes.CountDocuments(idFilter);
                //return cursor > 0;
            }
            public static void Delete(string guid, bool isComment, string key)
            {
                if (isComment)
                {
                    contextLite.Likes.Delete(d => d.Comment_Guid == guid & d.Owner_Guid == key);
                }
                else
                {
                    contextLite.Likes.Delete(d => d.Confess_Guid == guid & d.Owner_Guid == key);
                }

                //FilterDefinitionBuilder<Likes> builder = Builders<Likes>.Filter;
                //FilterDefinition<Likes> idFilter;
                //if (isComment)
                //{
                //    idFilter = builder.And(builder.Eq(f => f.Comment_Guid, guid), builder.Eq(v => v.Owner_Guid, key));
                //}
                //else
                //{
                //    idFilter = builder.And(builder.Eq(f => f.Confess_Guid, guid), builder.Eq(v => v.Owner_Guid, key));
                //}
                //context.Likes.DeleteMany(idFilter);
            }
            public static string GetCount(string guid, bool isComment)
            {
                int counter = 0;
                if (isComment)
                {
                    counter = contextLite.Likes.Count(d => d.Comment_Guid == guid);
                }
                else
                {
                    counter = contextLite.Likes.Count(d => d.Confess_Guid == guid);
                }
                return counter.ToString();

                //FilterDefinitionBuilder<Likes> builder = Builders<Likes>.Filter;
                //FilterDefinition<Likes> idFilter;
                //if (isComment)
                //{
                //    idFilter = builder.Eq(r => r.Comment_Guid, guid);
                //}
                //else
                //{
                //    idFilter = builder.Eq(r => r.Confess_Guid, guid);
                //}
                //long count = context.Likes.CountDocuments(idFilter);
                //return count.ToString();
            }
        }

        public static class DislikeClass
        {
            public static bool Post(string guid, bool isComment, string key)
            {

                //check if its liked
                if (LikeClass.CheckExistence(guid, isComment, key))
                {
                    return false;
                }

                //Check if there is already a Dislike by the user
                if (CheckExistence(guid, isComment, key))
                {
                    //delete the Dislike
                    Delete(guid, isComment, key);
                    return false;
                }

                //post new Dislike
                Dislikes Dislike = new Dislikes
                {
                    Owner_Guid = key,

                };
                if (isComment)
                {
                    Dislike.Comment_Guid = guid;
                }
                else
                {
                    Dislike.Confess_Guid = guid;
                }
                Dislike.Id = Logical.Setter(Dislike.Id);
                contextLite.Dislikes.Insert(Dislike);
                //.Dislikes.InsertOne(Dislike);
                return true;
            }
            public static bool CheckExistence(string guid, bool isComment, string key)
            {
                int count = 0;
                if (isComment)
                {
                    count = contextLite.Dislikes.Count(d => d.Comment_Guid == guid & d.Owner_Guid == key);
                }
                else
                {
                    count = contextLite.Dislikes.Count(d => d.Confess_Guid == guid & d.Owner_Guid == key);
                }
                return count > 0;

                //FilterDefinitionBuilder<Dislikes> builder = Builders<Dislikes>.Filter;
                //FilterDefinition<Dislikes> idFilter;
                //if (isComment)
                //{
                //    idFilter = builder.And(builder.Eq(f => f.Comment_Guid, guid), builder.Eq(v => v.Owner_Guid, key));
                //}
                //else
                //{
                //    idFilter = builder.And(builder.Eq(f => f.Confess_Guid, guid), builder.Eq(v => v.Owner_Guid, key));
                //}
                //long cursor = context.Dislikes.CountDocuments(idFilter);
                //return (cursor > 0);
            }
            public static void Delete(string guid, bool isComment, string key)
            {
                if (isComment)
                {
                    contextLite.Dislikes.Delete(d => d.Comment_Guid == guid & d.Owner_Guid == key);
                }
                else
                {
                    contextLite.Dislikes.Delete(d => d.Confess_Guid == guid & d.Owner_Guid == key);
                }
                //FilterDefinitionBuilder<Dislikes> builder = Builders<Dislikes>.Filter;
                //FilterDefinition<Dislikes> idFilter;
                //if (isComment)
                //{
                //    idFilter = builder.And(builder.Eq(f => f.Comment_Guid, guid), builder.Eq(v => v.Owner_Guid, key));
                //}
                //else
                //{
                //    idFilter = builder.And(builder.Eq(f => f.Confess_Guid, guid), builder.Eq(v => v.Owner_Guid, key));
                //}
                //context.Dislikes.DeleteMany(idFilter);
            }
            public static string GetCount(string guid, bool isComment)
            {
                int counter = 0;
                if (isComment)
                {
                    counter = contextLite.Dislikes.Count(d => d.Comment_Guid == guid);
                }
                else
                {
                    counter = contextLite.Dislikes.Count(d => d.Confess_Guid == guid);
                }
                return counter.ToString();

                //FilterDefinitionBuilder<Dislikes> builder = Builders<Dislikes>.Filter;
                //FilterDefinition<Dislikes> idFilter;
                //if (isComment)
                //{
                //    idFilter = builder.Eq(r => r.Comment_Guid, guid);
                //}
                //else
                //{
                //    idFilter = builder.Eq(r => r.Confess_Guid, guid);
                //}
                //long count = context.Dislikes.CountDocuments(idFilter);
                //return count.ToString();
            }
        }

        public static class SeenClass
        {
            public static void Post(string guid, string key)
            {
                //Check if there is already a Seen by the user
                if (!CheckExistence(guid, key))
                {
                    //post new Seen
                    Seen Seen = new Seen
                    {
                        Owner_Guid = key,
                        Confess_Guid = guid

                    };
                    Seen.Id = Logical.Setter(Seen.Id);
                    contextLite.Seen.Insert(Seen);
                    // context.Seen.InsertOne(Seen);
                }
            }
            public static bool CheckExistence(string guid, string key)
            {
                int count = contextLite.Seen.Count(d => d.Confess_Guid == guid & d.Owner_Guid == key);
                return count > 0;
                //FilterDefinitionBuilder<Seen> builder = Builders<Seen>.Filter;
                //FilterDefinition<Seen> idFilter = builder.And(builder.Eq(f => f.Confess_Guid, guid), builder.Eq(v => v.Owner_Guid, key));
                //long curs = context.Seen.CountDocuments(idFilter);
                //return curs > 0;
            }
            public static string GetCount(string guid)
            {
                return contextLite.Seen.Count(d => d.Confess_Guid == guid).ToString();
                //FilterDefinitionBuilder<Seen> builder = Builders<Seen>.Filter;
                //FilterDefinition<Seen> idFilter = builder.Eq(r => r.Confess_Guid, guid);
                //long count = context.Seen.CountDocuments(idFilter);
                //return count.ToString();
            }
        }
    }
}
