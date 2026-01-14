using System;
using System.Collections.Generic;

namespace QuizMonitor.DAL.Models;

public partial class Prefix
{
    public string BucketId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int Level { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Bucket Bucket { get; set; } = null!;
}
