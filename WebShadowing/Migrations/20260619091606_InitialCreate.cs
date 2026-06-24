using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebShadowing.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    course_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.course_id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    lesson_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    course_id = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lesson_order = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.lesson_id);
                    table.ForeignKey(
                        name: "FK_Lessons_Courses_course_id",
                        column: x => x.course_id,
                        principalTable: "Courses",
                        principalColumn: "course_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "User_Statistics",
                columns: table => new
                {
                    stat_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    total_sessions = table.Column<int>(type: "int", nullable: false),
                    average_score = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    streak_days = table.Column<int>(type: "int", nullable: false),
                    last_practice_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User_Statistics", x => x.stat_id);
                    table.ForeignKey(
                        name: "FK_User_Statistics_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users_Courses",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    course_id = table.Column<long>(type: "bigint", nullable: false),
                    enrolled_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Progress = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users_Courses", x => new { x.user_id, x.course_id });
                    table.ForeignKey(
                        name: "FK_Users_Courses_Courses_course_id",
                        column: x => x.course_id,
                        principalTable: "Courses",
                        principalColumn: "course_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Users_Courses_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lesson_Material",
                columns: table => new
                {
                    material_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    lesson_id = table.Column<long>(type: "bigint", nullable: false),
                    material_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    content_url = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lesson_Material", x => x.material_id);
                    table.ForeignKey(
                        name: "FK_Lesson_Material_Lessons_lesson_id",
                        column: x => x.lesson_id,
                        principalTable: "Lessons",
                        principalColumn: "lesson_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Practice_Sessions",
                columns: table => new
                {
                    session_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    lesson_id = table.Column<long>(type: "bigint", nullable: false),
                    started_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    completed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    overall_score = table.Column<decimal>(type: "decimal(5,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Practice_Sessions", x => x.session_id);
                    table.ForeignKey(
                        name: "FK_Practice_Sessions_Lessons_lesson_id",
                        column: x => x.lesson_id,
                        principalTable: "Lessons",
                        principalColumn: "lesson_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Practice_Sessions_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AI_Feedback",
                columns: table => new
                {
                    feedback_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    session_id = table.Column<long>(type: "bigint", nullable: false),
                    pronunciation_score = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    fluency_score = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    accuracy_score = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    feedback_text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_Feedback", x => x.feedback_id);
                    table.ForeignKey(
                        name: "FK_AI_Feedback_Practice_Sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "Practice_Sessions",
                        principalColumn: "session_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "User_Recordings",
                columns: table => new
                {
                    recording_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    session_id = table.Column<long>(type: "bigint", nullable: false),
                    audio_url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User_Recordings", x => x.recording_id);
                    table.ForeignKey(
                        name: "FK_User_Recordings_Practice_Sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "Practice_Sessions",
                        principalColumn: "session_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transcripts",
                columns: table => new
                {
                    transcript_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    recording_id = table.Column<long>(type: "bigint", nullable: false),
                    transcript_text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    confidence_score = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transcripts", x => x.transcript_id);
                    table.ForeignKey(
                        name: "FK_Transcripts_User_Recordings_recording_id",
                        column: x => x.recording_id,
                        principalTable: "User_Recordings",
                        principalColumn: "recording_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AI_Feedback_session_id",
                table: "AI_Feedback",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_Lesson_Material_lesson_id",
                table: "Lesson_Material",
                column: "lesson_id");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_course_id_lesson_order",
                table: "Lessons",
                columns: new[] { "course_id", "lesson_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Practice_Sessions_lesson_id",
                table: "Practice_Sessions",
                column: "lesson_id");

            migrationBuilder.CreateIndex(
                name: "IX_Practice_Sessions_user_id",
                table: "Practice_Sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Transcripts_recording_id",
                table: "Transcripts",
                column: "recording_id");

            migrationBuilder.CreateIndex(
                name: "IX_User_Recordings_session_id",
                table: "User_Recordings",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_User_Statistics_user_id",
                table: "User_Statistics",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Courses_course_id",
                table: "Users_Courses",
                column: "course_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AI_Feedback");

            migrationBuilder.DropTable(
                name: "Lesson_Material");

            migrationBuilder.DropTable(
                name: "Transcripts");

            migrationBuilder.DropTable(
                name: "User_Statistics");

            migrationBuilder.DropTable(
                name: "Users_Courses");

            migrationBuilder.DropTable(
                name: "User_Recordings");

            migrationBuilder.DropTable(
                name: "Practice_Sessions");

            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Courses");
        }
    }
}
