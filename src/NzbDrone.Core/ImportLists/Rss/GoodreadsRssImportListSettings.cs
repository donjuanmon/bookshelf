using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.Goodreads.Rss
{
    public class GoodreadsRssImportListSettingsValidator : AbstractValidator<GoodreadsRssImportListSettings>
    {
        public GoodreadsRssImportListSettingsValidator()
        {
            RuleFor(c => c.RssUrl).NotEmpty();
            RuleFor(c => c.RssUrl).Matches(@"^https?://.*goodreads\.com/.*")
                .WithMessage("Must be a valid Goodreads RSS URL");
        }
    }

    public class GoodreadsRssImportListSettings : IImportListSettings
    {
        private static readonly GoodreadsRssImportListSettingsValidator Validator = new ();

        public string BaseUrl { get; set; }

        [FieldDefinition(0, Label = "RSS URL", HelpText = "Goodreads RSS feed URL (e.g., https://www.goodreads.com/review/list_rss/USER_ID?shelf=to-read)")]
        public string RssUrl { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
