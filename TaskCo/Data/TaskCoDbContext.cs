using Microsoft.EntityFrameworkCore;
using TaskCo.Models;

namespace TaskCo.Data;

public class TaskCoDbContext(DbContextOptions<TaskCoDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
}
