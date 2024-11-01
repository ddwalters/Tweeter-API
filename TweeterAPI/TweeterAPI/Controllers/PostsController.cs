﻿using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using TweeterAPI.Models;

namespace TweeterAPI.Controllers
{
    [Route("api/Posts")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly string _connectionString;

        public PostsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("NotTwitter_DBConnection");
        }

        [HttpPost(Name = "CreatePosts")]
        public async Task<IActionResult> Create([FromBody] Posts posts)
        {
            string commandText = @"INSERT INTO Posts (account_id, posts_message, posts_likes, upload_time)
                                  VALUES((SELECT account_id FROM Accounts WHERE account_id = @account_id), @posts_message, @posts_likes, @upload_time);";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(commandText, conn))
                {
                    conn.Open();

                    try
                    {
                        cmd.Parameters.Add("@account_id", SqlDbType.UniqueIdentifier);
                        cmd.Parameters["@account_id"].Value = posts.AccountId;

                        cmd.Parameters.Add("@posts_message", SqlDbType.NVarChar);
                        cmd.Parameters["@posts_message"].Value = posts.Message;

                        cmd.Parameters.Add("@posts_likes", SqlDbType.Int);
                        cmd.Parameters["@posts_likes"].Value = posts.Likes;

                        cmd.Parameters.Add("@upload_time", SqlDbType.DateTime);
                        cmd.Parameters["@upload_time"].Value = DateTime.Now;

                        cmd.ExecuteNonQuery();
                        return Ok("success");
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(ex.Message);
                    }
                }
            }
        }

        [HttpGet(Name = "GetPosts")]
        public IEnumerable<Posts> Get()
        {
            var _posts = new List<Posts>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(@"
SELECT 
	p.posts_id, 
	a.account_id, 
	a.account_name, 
	p.posts_message, 
	p.posts_likes, 
	count(c.source_id) AS replies_count, 
	p.upload_time
FROM 
	Posts p
	JOIN Accounts a ON a.account_id = p.account_id
	LEFT JOIN Comments c ON c.source_id = p.posts_id
GROUP BY 
	p.posts_id, 
	a.account_id,
	a.account_name, 
	p.posts_message, 
	p.posts_likes, 
	p.upload_time
ORDER BY upload_time DESC;", conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        _posts.Add(new Posts
                        {
                            Id = (Guid)reader["posts_id"],
                            AccountId = (Guid)reader["account_id"],
                            AccountName = reader["account_name"].ToString(),
                            Message = reader["posts_message"].ToString(),
                            Likes = (int)reader["posts_likes"],
                            Replies = (int)reader["replies_count"],
                            UploadTime = (DateTime)reader["upload_time"],
                        });
                    }
                }
            }

            return _posts;
        }

        [HttpPut(Name = "UpdatePosts")]
        public async Task<IActionResult> Update([FromBody] Posts posts)
        {
            string commandText = $"UPDATE Posts SET posts_message = @posts_message, posts_likes = @posts_likes WHERE posts_id = @posts_id";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(commandText, conn))
                {
                    conn.Open();

                    try
                    {
                        cmd.Parameters.Add("@posts_id", SqlDbType.UniqueIdentifier);
                        cmd.Parameters["@posts_id"].Value = posts.Id;

                        cmd.Parameters.Add("@posts_message", SqlDbType.NVarChar);
                        cmd.Parameters["@posts_message"].Value = posts.Message;

                        cmd.Parameters.Add("@posts_likes", SqlDbType.NVarChar);
                        cmd.Parameters["@posts_likes"].Value = posts.Likes;

                        cmd.ExecuteNonQuery();

                        return Ok("success");
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(ex.Message);
                    }
                }
            }
        }

        [HttpDelete(Name = "DeletePosts")]
        public async Task<IActionResult> Delete([FromBody] Posts posts)
        {
            string commandText = "DELETE FROM Posts WHERE posts_id LIKE @posts_id";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(commandText, conn))
                {
                    conn.Open();

                    cmd.Parameters.Add("@posts_id", SqlDbType.UniqueIdentifier);
                    cmd.Parameters["@posts_id"].Value = posts.Id;

                    cmd.ExecuteNonQuery();
                    return Ok("success");
                }
            }
        }
    }
}
