namespace Venues.Domain;

/// <summary>
/// One numbered revision of a <see cref="SeatMap"/> — the sections, rows, seats, admission areas
/// and drawn elements as they stood when this version was published.
/// </summary>
/// <remarks>
/// <b>Versions exist because seats are sold against them.</b> A venue reconfigures: a block is
/// removed for a stage extension, a row is renumbered, a standing area replaces seating. If the map
/// were edited in place, every ticket already sold would silently start referring to a different
/// place — or to nowhere. Instead a published version is frozen, a structural change starts a new
/// one, and tickets keep resolving against the version they were sold under.
/// </remarks>
public sealed class SeatMapVersion
{
    private readonly List<VenueSection> _sections = new();
    private readonly List<AdmissionArea> _admissionAreas = new();
    private readonly List<SeatMapElement> _elements = new();

    internal SeatMapVersion(Guid id, Guid seatMapId, int versionNumber)
    {
        Id = id;
        SeatMapId = seatMapId;
        VersionNumber = versionNumber;
        Status = SeatMapVersionStatus.Draft;
    }

    // Parameterless ctor for EF Core materialization.
    private SeatMapVersion()
    {
    }

    /// <summary>Unique version id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>The seat map this is a version of.</summary>
    public Guid SeatMapId { get; private set; }

    /// <summary>Version number, starting at 1 and increasing by one.</summary>
    public int VersionNumber { get; private set; }

    /// <summary>Lifecycle state.</summary>
    public SeatMapVersionStatus Status { get; private set; }

    /// <summary>When this version was published, if it has been.</summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    /// <summary>Reserved-seating sections.</summary>
    public IReadOnlyCollection<VenueSection> Sections => _sections;

    /// <summary>Unreserved capacity areas.</summary>
    public IReadOnlyCollection<AdmissionArea> AdmissionAreas => _admissionAreas;

    /// <summary>The graphical layer.</summary>
    public IReadOnlyCollection<SeatMapElement> Elements => _elements;

    /// <summary>
    /// Total sellable capacity — sellable seats plus admission-area capacity. What an event's
    /// inventory is provisioned from.
    /// </summary>
    public int Capacity => _sections.Sum(s => s.SellableSeatCount) + _admissionAreas.Sum(a => a.Capacity);

    /// <summary>
    /// Replaces the whole layout. Only a <see cref="SeatMapVersionStatus.Draft"/> accepts this;
    /// see <see cref="SeatMapLayout"/> for why the protocol is replacement rather than patching.
    /// </summary>
    /// <param name="layout">The complete layout to store.</param>
    /// <exception cref="InvalidOperationException">
    /// The version is not a draft, or an element names a section or area code the layout does not
    /// contain.
    /// </exception>
    public void ReplaceLayout(SeatMapLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        EnsureDraft();

        _sections.Clear();
        _admissionAreas.Clear();
        _elements.Clear();

        var sectionIdsByCode = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var areaIdsByCode = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var sectionDraft in layout.Sections)
        {
            var section = new VenueSection(
                Guid.CreateVersion7(),
                Id,
                sectionDraft.Code,
                sectionDraft.Name,
                sectionDraft.DisplayOrder,
                sectionDraft.GateId,
                sectionDraft.TierLabel);

            foreach (var rowDraft in sectionDraft.Rows)
            {
                var row = section.AddRow(rowDraft.Label, rowDraft.DisplayOrder);
                foreach (var seatDraft in rowDraft.Seats)
                {
                    row.AddSeat(seatDraft.Number, seatDraft.Attributes, seatDraft.IsSellable);
                }
            }

            _sections.Add(section);
            sectionIdsByCode.TryAdd(sectionDraft.Code, section.Id);
        }

        foreach (var areaDraft in layout.AdmissionAreas)
        {
            var area = new AdmissionArea(
                Guid.CreateVersion7(),
                Id,
                areaDraft.Code,
                areaDraft.Name,
                areaDraft.Capacity,
                areaDraft.DisplayOrder,
                areaDraft.GateId,
                areaDraft.TierLabel);

            _admissionAreas.Add(area);
            areaIdsByCode.TryAdd(areaDraft.Code, area.Id);
        }

        foreach (var elementDraft in layout.Elements)
        {
            _elements.Add(BuildElement(elementDraft, sectionIdsByCode, areaIdsByCode));
        }
    }

    /// <summary>
    /// Everything wrong with this version, as a list — empty means it is publishable.
    /// </summary>
    /// <remarks>
    /// Gate references are not checked here. A gate belongs to the venue, not to the map, and an
    /// aggregate that reached across to validate one would be reading another aggregate's state to
    /// decide its own. The application layer checks them against
    /// <see cref="Venue.HasActiveGate(Guid)"/> before calling <see cref="Publish"/>.
    /// </remarks>
    /// <returns>The validation errors, or an empty list.</returns>
    public IReadOnlyList<SeatMapValidationError> Validate()
    {
        var errors = new List<SeatMapValidationError>();

        ValidateCodesAreUnique(errors);
        ValidateSections(errors);
        ValidateAdmissionAreas(errors);
        ValidateGeometry(errors);

        if (Capacity == 0)
        {
            errors.Add(new SeatMapValidationError(
                "empty_layout",
                "The map sells nothing: it has no sellable seats and no admission-area capacity."));
        }

        return errors;
    }

    /// <summary>
    /// Freezes this version. After this the layout is immutable and a structural change needs a new
    /// version.
    /// </summary>
    /// <param name="publishedAt">The publication instant.</param>
    /// <exception cref="InvalidOperationException">
    /// The version is not a draft, or it does not pass <see cref="Validate"/>. Callers should call
    /// <see cref="Validate"/> first and report the list; this throw is the guard that keeps an
    /// invalid map from being frozen by a caller that forgot.
    /// </exception>
    public void Publish(DateTimeOffset publishedAt)
    {
        EnsureDraft();

        var errors = Validate();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"The seat map cannot be published: {string.Join("; ", errors.Select(e => e.Message))}");
        }

        Status = SeatMapVersionStatus.Published;
        PublishedAt = publishedAt;
    }

    /// <summary>Marks a published version as replaced by a later one.</summary>
    /// <exception cref="InvalidOperationException">The version was never published.</exception>
    public void Supersede()
    {
        if (Status != SeatMapVersionStatus.Published)
        {
            throw new InvalidOperationException("Only a published version can be superseded.");
        }

        Status = SeatMapVersionStatus.Superseded;
    }

    /// <summary>
    /// Describes this version's layout in the shape a new draft is built from — how a structural
    /// change starts from what is live rather than from an empty canvas.
    /// </summary>
    /// <returns>The layout, with codes rather than ids so it can be rebuilt with fresh identities.</returns>
    public SeatMapLayout ToLayout()
    {
        var sectionCodesById = _sections.ToDictionary(s => s.Id, s => s.Code);
        var areaCodesById = _admissionAreas.ToDictionary(a => a.Id, a => a.Code);

        var sections = _sections
            .OrderBy(s => s.DisplayOrder)
            .Select(section => new SectionDraft(
                section.Code,
                section.Name,
                section.DisplayOrder,
                section.GateId,
                section.Rows
                    .OrderBy(r => r.DisplayOrder)
                    .Select(row => new SeatRowDraft(
                        row.Label,
                        row.DisplayOrder,
                        row.Seats.Select(s => new SeatDraft(s.Number, s.Attributes, s.IsSellable)).ToList()))
                    .ToList(),
                section.TierLabel))
            .ToList();

        var areas = _admissionAreas
            .OrderBy(a => a.DisplayOrder)
            .Select(area => new AdmissionAreaDraft(
                area.Code,
                area.Name,
                area.Capacity,
                area.DisplayOrder,
                area.GateId,
                area.TierLabel))
            .ToList();

        var elements = _elements
            .Select(element => new SeatMapElementDraft(
                element.Kind,
                element.Shape,
                element.X,
                element.Y,
                element.Width,
                element.Height,
                element.Rotation,
                element.Label,
                element.PointsJson,
                element.StyleJson,
                element.VenueSectionId is null ? null : sectionCodesById.GetValueOrDefault(element.VenueSectionId.Value),
                element.AdmissionAreaId is null ? null : areaCodesById.GetValueOrDefault(element.AdmissionAreaId.Value)))
            .ToList();

        return new SeatMapLayout(sections, areas, elements);
    }

    /// <summary>Every gate id this version's sections and areas route through.</summary>
    /// <returns>The distinct gate ids referenced.</returns>
    public IReadOnlySet<Guid> ReferencedGateIds() =>
        _sections.Select(s => s.GateId)
            .Concat(_admissionAreas.Select(a => a.GateId))
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .ToHashSet();

    private static void ValidateRow(VenueSection section, SeatRow row, List<SeatMapValidationError> errors)
    {
        if (row.Seats.Count == 0)
        {
            errors.Add(new SeatMapValidationError(
                "row_without_seats",
                $"Row '{row.Label}' in section '{section.Code}' has no seats."));
            return;
        }

        var duplicateSeats = row.Seats
            .GroupBy(s => s.Number, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var number in duplicateSeats)
        {
            errors.Add(new SeatMapValidationError(
                "duplicate_seat_number",
                $"Row '{row.Label}' in section '{section.Code}' has more than one seat numbered '{number}'."));
        }
    }

    private static void ValidateElementGeometry(SeatMapElement element, List<SeatMapValidationError> errors)
    {
        var needsPoints = element.Shape is SeatMapElementShape.Polygon or SeatMapElementShape.Path;

        if (needsPoints && string.IsNullOrWhiteSpace(element.PointsJson))
        {
            errors.Add(new SeatMapValidationError(
                "missing_element_points",
                $"A {element.Shape} element ('{element.Label ?? element.Kind.ToString()}') has no points."));
        }

        if (!needsPoints && (element.Width <= 0 || element.Height <= 0))
        {
            errors.Add(new SeatMapValidationError(
                "missing_element_bounds",
                $"A {element.Shape} element ('{element.Label ?? element.Kind.ToString()}') has no width or height."));
        }
    }

    private SeatMapElement BuildElement(
        SeatMapElementDraft draft,
        Dictionary<string, Guid> sectionIdsByCode,
        Dictionary<string, Guid> areaIdsByCode)
    {
        Guid? sectionId = null;
        if (draft.SectionCode is not null)
        {
            if (!sectionIdsByCode.TryGetValue(draft.SectionCode, out var resolved))
            {
                throw new InvalidOperationException(
                    $"An element refers to section '{draft.SectionCode}', which this layout does not contain.");
            }

            sectionId = resolved;
        }

        Guid? areaId = null;
        if (draft.AdmissionAreaCode is not null)
        {
            if (!areaIdsByCode.TryGetValue(draft.AdmissionAreaCode, out var resolved))
            {
                throw new InvalidOperationException(
                    $"An element refers to admission area '{draft.AdmissionAreaCode}', which this layout does not contain.");
            }

            areaId = resolved;
        }

        return new SeatMapElement(
            Guid.CreateVersion7(),
            Id,
            draft.Kind,
            draft.Shape,
            draft.X,
            draft.Y,
            draft.Width,
            draft.Height,
            draft.Rotation,
            draft.Label,
            draft.PointsJson,
            draft.StyleJson,
            sectionId,
            areaId);
    }

    private void ValidateCodesAreUnique(List<SeatMapValidationError> errors)
    {
        var duplicates = _sections.Select(s => s.Code)
            .Concat(_admissionAreas.Select(a => a.Code))
            .GroupBy(code => code, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var code in duplicates)
        {
            errors.Add(new SeatMapValidationError(
                "duplicate_code",
                $"Code '{code}' is used by more than one section or admission area."));
        }
    }

    private void ValidateSections(List<SeatMapValidationError> errors)
    {
        foreach (var section in _sections)
        {
            if (section.Rows.Count == 0)
            {
                errors.Add(new SeatMapValidationError(
                    "section_without_rows",
                    $"Section '{section.Code}' has no rows. Add rows, or make it an admission area."));
                continue;
            }

            var duplicateRows = section.Rows
                .GroupBy(r => r.Label, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);

            foreach (var label in duplicateRows)
            {
                errors.Add(new SeatMapValidationError(
                    "duplicate_row_label",
                    $"Section '{section.Code}' has more than one row labelled '{label}'."));
            }

            foreach (var row in section.Rows)
            {
                ValidateRow(section, row, errors);
            }
        }
    }

    private void ValidateAdmissionAreas(List<SeatMapValidationError> errors)
    {
        foreach (var area in _admissionAreas.Where(a => a.Capacity <= 0))
        {
            errors.Add(new SeatMapValidationError(
                "invalid_area_capacity",
                $"Admission area '{area.Code}' has a capacity of {area.Capacity.ToString(CultureInfo.InvariantCulture)}."));
        }
    }

    // A map is allowed to be purely logical — a small hall needs no plan, and forcing one would be
    // busywork. But a map that is *partly* drawn is worse than one that is not drawn at all: the
    // buyer sees a plan with a hole in it and cannot tell whether the missing block is sold out or
    // simply missing. So the rule is all or nothing.
    private void ValidateGeometry(List<SeatMapValidationError> errors)
    {
        if (_elements.Count == 0)
        {
            return;
        }

        var drawnSectionIds = _elements.Where(e => e.VenueSectionId is not null).Select(e => e.VenueSectionId!.Value).ToHashSet();
        var drawnAreaIds = _elements.Where(e => e.AdmissionAreaId is not null).Select(e => e.AdmissionAreaId!.Value).ToHashSet();

        foreach (var section in _sections.Where(s => !drawnSectionIds.Contains(s.Id)))
        {
            errors.Add(new SeatMapValidationError(
                "section_not_drawn",
                $"Section '{section.Code}' has no shape on a map that draws the others."));
        }

        foreach (var area in _admissionAreas.Where(a => !drawnAreaIds.Contains(a.Id)))
        {
            errors.Add(new SeatMapValidationError(
                "area_not_drawn",
                $"Admission area '{area.Code}' has no shape on a map that draws the others."));
        }

        foreach (var element in _elements)
        {
            ValidateElementGeometry(element, errors);
        }
    }

    private void EnsureDraft()
    {
        if (Status != SeatMapVersionStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Version {VersionNumber.ToString(CultureInfo.InvariantCulture)} is {Status} and cannot be changed. Start a new version instead.");
        }
    }
}
