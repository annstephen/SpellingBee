using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpellingBee.Words.Domain;

namespace SpellingBee.Words.Data.Configurations;

internal sealed class WordConfiguration : IEntityTypeConfiguration<Word>
{
    public void Configure(EntityTypeBuilder<Word> builder)
    {
        builder.ToTable("Words");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Text)
               .IsRequired()
               .HasMaxLength(200);
        builder.HasIndex(w => w.Text)
               .IsUnique();
        builder.Property(w => w.PartOfSpeech).HasMaxLength(100);
        builder.Property(w => w.Definition).HasMaxLength(2000);
        builder.Property(w => w.Etymology).HasMaxLength(2000);
        builder.Property(w => w.AudioKey).HasMaxLength(200);
        builder.Property(w => w.AudioFilePath).HasMaxLength(500);
    }
}
