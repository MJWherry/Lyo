"use client";

export { LyoProvider } from "./provider/LyoProvider.js";
export type { LyoProviderProps } from "./provider/LyoProvider.js";
export { useLyoSnackbar } from "./provider/LyoSnackbar.js";
export { useLyoDialog, LyoDialogPresets } from "./provider/LyoDialogContext.js";
export { clientStore } from "./provider/clientStore.js";
export {
  normalizeElementIdSegment,
  dataGridElementId,
  dataGridProjectedElementId,
  resolveElementId,
} from "./provider/elementId.js";
export { createLyoTheme, detectColorMode } from "./theme/createLyoTheme.js";
export { statusColor, statusIcon } from "./status/status.js";

export {
  LyoButton,
  LyoIconButton,
  LyoTextField,
  LyoNumericField,
  LyoSwitch,
  LyoCheckbox,
  LyoSelect,
  LyoAutocomplete,
  LyoAlert,
  LyoTooltip,
  LyoMenu,
  LyoMenuItem,
  LyoTabs,
  LyoTab,
  LyoDivider,
  LyoStack,
  LyoGrid,
  LyoCard,
  LyoPaper,
  LyoContainer,
  LyoAppBar,
  LyoDrawer,
  LyoSkeleton,
  LyoSpacer,
  LyoProgress,
  LyoDatePicker,
  LyoFormControl,
  LyoInputLabel,
} from "./primitives/primitives.js";

export {
  toUtcMs,
  latestUtcMs,
  formatRelative,
  formatKind,
  formatAbsolute,
  timeZoneLabel,
  browserTimeZone,
  DEFAULT_RELATIVE_WINDOW_MS,
} from "./time/timestamp.js";
export type { LyoTimestampKind } from "./time/timestamp.js";
export { formatDuration, formatClock, durationColor, durationMsBetween, formatDurationFromDates } from "./time/duration.js";
export type { LyoDurationColor } from "./time/duration.js";
export { LyoTimestamp, LyoDuration, useBrowserTimeZone, useNow } from "./time/LyoTimestamp.js";
export type { LyoTimestampProps, LyoDurationProps } from "./time/LyoTimestamp.js";
export {
  forMonth,
  fromTimeFrames,
  withTimeZone,
  formatCalendarClock,
  formatCalendarOffset,
  civilToUtc,
  calendarTimeZones,
} from "./calendar/calendar.js";
export type { CalendarModel, CalendarDay, CalendarEntry, CalendarEntityRef, TimeFrame } from "./calendar/calendar.js";
export { LyoCalendar } from "./calendar/LyoCalendar.js";
export type { LyoCalendarProps } from "./calendar/LyoCalendar.js";

export { HEATMAP_EMPTY, HEATMAP_HUE, aggregateHeatmap, heatFill, heatmapView } from "./map/heatmap.js";
export type { HeatmapModel, HeatmapRow, HeatmapView, HeatmapViewRegion } from "./map/heatmap.js";
export { HEATMAP_COUNTY_QUERY_AMOUNT, HEATMAP_STATE_QUERY_AMOUNT, heatmapCountQuery, heatmapRowsFromCounts } from "./map/heatmapQuery.js";
export type { HeatmapCountRows } from "./map/LyoUsHeatmap.js";
export { LyoUsHeatmap } from "./map/LyoUsHeatmap.js";
export type { HeatmapSelection, LyoUsHeatmapProps } from "./map/LyoUsHeatmap.js";

export { abbreviateId, idFieldCanToggle, DEFAULT_ID_ABBREVIATION_LENGTH } from "./display/idField.js";
export type { LyoIdAbbreviation } from "./display/idField.js";
export { LyoIdField } from "./display/LyoIdField.js";
export type { LyoIdFieldProps } from "./display/LyoIdField.js";
export { useCopyToClipboard } from "./display/useCopyToClipboard.js";
export { splitHighlight, truncateText } from "./display/highlight.js";
export type { HighlightSegment } from "./display/highlight.js";
export { LyoTruncatedText, LyoHighlighter } from "./display/LyoText.js";
export { chipOf, chipFromBool, chipFromEnum, hueColor, normalizeHue, LyoChipHues, LyoChipHueList } from "./display/chips.js";
export type { LyoChipSpec, LyoChipColor, LyoChipVariant, LyoChipIconKey, LyoChipHueName } from "./display/chips.js";
export { normalizeStatus, humanizeStatus, resolveDefaultStatus, createStatusPaletteResolver } from "./display/status.js";
export type { LyoStatusPalette, LyoStatusPaletteResolver } from "./display/status.js";
export { LyoChip, LyoChips, LyoStatusChip } from "./display/LyoChip.js";
export type { LyoChipProps, LyoStatusChipProps } from "./display/LyoChip.js";
export { withLineNumbers } from "./display/codeBlock.js";
export { LyoCodeBlock } from "./display/LyoCodeBlock.js";
export type { LyoCodeBlockProps } from "./display/LyoCodeBlock.js";
export { LyoExpandableCard } from "./display/LyoExpandableCard.js";
export type { LyoExpandableCardProps } from "./display/LyoExpandableCard.js";
export { normalizeMediaSrc, imageSrc, resolveCaptions, htmlCaptionKind, MediaCaptionKinds } from "./media/media.js";
export type { MediaCaptionKind, MediaCaptionTrack } from "./media/media.js";
export { LyoImage, LyoAudio, LyoVideo } from "./media/LyoMedia.js";
export type { LyoImageProps, LyoAudioProps, LyoVideoProps } from "./media/LyoMedia.js";

export {
  LyoToolbar,
  LyoToolbarRow,
  LyoToolbarGroup,
  LyoToolbarButton,
  LyoToolbarIconButton,
  LyoToolbarMenu,
  LyoToolbarField,
  LyoToolbarSeparator,
} from "./shell/LyoToolbar.js";
export type { LyoToolbarProps, LyoToolbarRowProps, LyoToolbarButtonProps, LyoToolbarIconButtonProps, LyoToolbarMenuProps } from "./shell/LyoToolbar.js";
export { LyoPageHeader, LyoBreadcrumbs, LyoSection } from "./shell/LyoPage.js";
export type { LyoBreadcrumb, LyoPageHeaderProps, LyoSectionProps } from "./shell/LyoPage.js";
export { slugTab, resolveActiveTab } from "./shell/tabs.js";
export { LyoTabSet } from "./shell/LyoTabSet.js";
export type { LyoTabSetProps, LyoTabDefinition } from "./shell/LyoTabSet.js";

export { resolveResultPhase, formatResultErrors } from "./state/resultPhase.js";
export type { LyoResultPhase, LyoResultLike, LyoResultError } from "./state/resultPhase.js";
export { LyoEmptyState, LyoSkeletonPanel, LyoResultBoundary } from "./state/LyoState.js";
export type { LyoEmptyStateProps, LyoSkeletonPanelProps, LyoResultBoundaryProps } from "./state/LyoState.js";
export { LYO_LOG_LEVELS, filterLogEntries, formatLogEntries, logLevelColor } from "./state/log.js";
export type { LyoLogEntry, LyoLogLevel } from "./state/log.js";
export { LyoLogViewer } from "./state/LyoLogViewer.js";
export type { LyoLogViewerProps } from "./state/LyoLogViewer.js";

export { LYO_BREAKPOINTS, breakpointOf, isAtOrBelow } from "./responsive/viewport.js";
export type { LyoBreakpoint } from "./responsive/viewport.js";
export { useViewport, LyoResponsiveTable, LyoRecordCard, LyoRecordField } from "./responsive/LyoResponsive.js";
export type { LyoResponsiveLayout, LyoResponsiveTableProps, LyoRecordCardProps, LyoRecordFieldProps } from "./responsive/LyoResponsive.js";

export { LyoHoverPopover } from "./overlay/LyoHoverPopover.js";
export type { LyoHoverPopoverProps } from "./overlay/LyoHoverPopover.js";
export { LyoDialog } from "./overlay/LyoDialog.js";
export type { LyoDialogProps } from "./overlay/LyoDialog.js";
export { LyoJsonViewDialog, useJsonViewDialog } from "./overlay/LyoJsonViewDialog.js";
export type { LyoJsonViewOptions } from "./overlay/LyoJsonViewDialog.js";
export { WhereClauseViewDialog } from "./overlay/WhereClauseViewDialog.js";

export { ChipInput } from "./query/ChipInput.js";
export type { ChipInputProps } from "./query/ChipInput.js";
export { WhereClauseBuilder } from "./query/WhereClauseBuilder.js";
export type { WhereClauseBuilderProps } from "./query/WhereClauseBuilder.js";
export {
  QueryBuilder,
  createDefaultQueryBuilderValue,
  activeRequestPreview,
} from "./query/QueryBuilder.js";
export { QueryBuilder as QueryRequestBuilder } from "./query/QueryBuilder.js";
export type { QueryBuilderProps, QueryBuilderValue } from "./query/QueryBuilder.js";
export { UniqueValueSelector } from "./query/UniqueValueSelector.js";
export type { UniqueValueSelectorProps } from "./query/UniqueValueSelector.js";
export { compactMenuProps, LYO_SELECT_MENU_MAX_HEIGHT } from "./query/selectMenu.js";
export { filterUniqueValues, selectAllVisible, clearVisible, uniqueValueKey } from "./query/uniqueValues.js";
export { FilterChipLabel, formatFilterChip, DEFAULT_FILTER_CHIP_MAX_LENGTH } from "./query/FilterChipLabel.js";
export type { FilterChipLabelProps } from "./query/FilterChipLabel.js";
export { formatFilterValue, filterChipText, appendFilterUnique, FUZZY_THRESHOLD_HELP } from "./query/filterChipText.js";
export { QueryFilterComponent } from "./query/QueryFilterComponent.js";
export type { QueryFilterComponentProps, QueryFilterHandle } from "./query/QueryFilterComponent.js";
export { QueryNodeEditor } from "./query/QueryNodeEditor.js";
export type { QueryNodeEditorProps } from "./query/QueryNodeEditor.js";
export {
  addChildAt,
  addSubClauseAt,
  fieldNameError,
  moveInto,
  nodeAt,
  nodeLabel,
  removeAt as removeWhereNodeAt,
  removeSubClauseAt,
  setDescriptionAt,
  updateAt,
} from "./query/whereTree.js";
export type { WherePath } from "./query/whereTree.js";
export {
  QueryConcreteForm,
  QueryProjectionForm,
  QueryRootForm,
  QueryScoreChip,
  QueryKeysEditor,
  AggregatesEditor,
  ComputedFieldsEditor,
  SortByEditor,
  toRootQuerySelect,
} from "./query/QueryForms.js";
export type { QueryFormPresets, QueryFormProps } from "./query/QueryForms.js";
export {
  addComputedField,
  addJoinOn,
  formatKeySet,
  keysFromLines,
  parseKeyInput,
  patchJoin,
  patchJoinOn,
  removeJoinOn,
  withComputedFields,
  withKeys,
  withZip,
  zipEnabled,
} from "./query/requestShape.js";
export { LyoParameterOptionsSelect, LyoParameterOptionsEditor } from "./query/ParamOptions.js";
export type { LyoParameterOptionsSelectProps, LyoParameterOptionsEditorProps, ParameterOptionsItem } from "./query/ParamOptions.js";
export {
  PARAMETER_OPTIONS_KINDS,
  allowedValuesFromOptions,
  bindQueryOptions,
  bindSprocParameters,
  createDefaultOptionsJson,
  defaultOptionsQuery,
  isValidStoredProcName,
  optionsEditorError,
  optionsFromAllowedValues,
  optionsKindOf,
  parseParameterOptions,
  pruneSelection,
  queryBaseRoute,
  queryPlaceholderKeys,
  readKeyValue,
  serializeParameterOptions,
  sprocPlaceholderKeys,
} from "./params/paramOptions.js";
export type { BindResult, ParameterOptions, ParameterOptionsDraft, ParameterOptionsKind, SiblingValues } from "./params/paramOptions.js";
export {
  addParameterRow,
  applyCreated,
  deleteParameterRow,
  dropInsertIndex,
  editRowErrors,
  editRowFromMasked,
  effectiveOptions,
  effectiveOptionsKind,
  entriesFrom,
  entryIsEmpty,
  entryShouldSubmit,
  entrySubmitValue,
  moveParameterRow,
  newEditRow,
  requiredSatisfied,
  rowForSave,
  saveParameterRows,
  setRowEncrypt,
  setRowKey,
  setRowOptions,
  setRowType,
  siblingMap,
} from "./params/editRow.js";
export type {
  LyoParameterCreated,
  LyoParameterDefaultKind,
  LyoParameterDefinition,
  LyoParameterEditRow,
  LyoParameterEntry,
  LyoParameterRowDto,
  LyoParameterSaveHandlers,
} from "./params/editRow.js";
export {
  LyoKeyedValueTable,
  LyoParameterCard,
  LyoParameterDetails,
  LyoParameterEditor,
  LyoParameterFlags,
  LyoParameterKeyField,
  LyoParameterValueField,
  LyoParameterValueGrid,
} from "./params/LyoParameterEditor.js";
export type {
  LyoKeyedValue,
  LyoKeyedValueTableProps,
  LyoParameterCardProps,
  LyoParameterDetailsProps,
  LyoParameterEditorProps,
  LyoParameterFlagsProps,
  LyoParameterKeyFieldProps,
  LyoParameterLayout,
  LyoParameterOptionSources,
  LyoParameterValueFieldProps,
  LyoParameterValueGridProps,
} from "./params/LyoParameterEditor.js";
export { QueryWorkbench, WORKBENCH_SAVE_DEBOUNCE_MS, buildRunUrl, buildSqlUrl, authHeaders } from "./query/QueryWorkbench.js";
export type { QueryWorkbenchProps, QueryWorkbenchRunContext } from "./query/QueryWorkbench.js";
export { QueryBuilderSettings } from "./query/QueryBuilderSettings.js";
export type { QueryBuilderSettingsProps } from "./query/QueryBuilderSettings.js";
export { QueryRunPanel } from "./query/QueryRunPanel.js";
export type { QueryRunPanelProps } from "./query/QueryRunPanel.js";
export {
  WORKBENCH_SAVE_DEBOUNCE_MS as queryWorkbenchSaveDebounceMs,
  normalizeBaseUrl,
  normalizeRun,
  defaultRunConfig,
  restoreWorkbenchState,
} from "./query/workbenchRun.js";
export type {
  QueryWorkbenchAuthMode,
  QueryWorkbenchRunConfig,
  QueryWorkbenchPersistedState,
} from "./query/workbenchRun.js";

export { asLyoQueryClient, createBffQueryClient } from "./client/LyoQueryClient.js";
export type { LyoQueryClient } from "./client/LyoQueryClient.js";

export {
  LyoDataGridFeatureFlags,
  hasFeature,
  createLyoColumn,
  createLyoIdColumn,
  projectedValue,
  projectedField,
  projectedId,
} from "./data-grid/types.js";
export type {
  LyoColumn,
  LyoColumnAlign,
  LyoRelatedLookup,
  LyoRelatedClient,
  LyoRelatedQueryProject,
  FilterState,
  LyoDataGridPersistedState,
  LyoDataGridMode,
} from "./data-grid/types.js";
export {
  DEFAULT_MAX_BULK_SIZE,
  bulkLabel,
  canExport,
  cardColumns,
  isIdField,
  quickSearchPlaceholder,
  quickSearchProperties,
  resolveCardMode,
  selectGrew,
} from "./data-grid/gridModel.js";
export type { LyoDataGridLayout, LyoCardColumn, LyoBulkState } from "./data-grid/gridModel.js";
export { loadRelatedEntities, lookupRelated, relatedGroups } from "./data-grid/relatedLookup.js";
export type { RelatedLookup, RelatedLoadGroup } from "./data-grid/relatedLookup.js";
export { useLyoDataGrid } from "./data-grid/useLyoDataGrid.js";
export type { UseLyoDataGridOptions, LyoDataGridController } from "./data-grid/useLyoDataGrid.js";
export { LyoDataGrid, LyoDataGridProjected, defaultPersonGridColumns } from "./data-grid/LyoDataGrid.js";
export type { LyoDataGridProps } from "./data-grid/LyoDataGrid.js";
export { LyoDataGridCards } from "./data-grid/LyoDataGridCards.js";
export type { LyoDataGridCardsProps } from "./data-grid/LyoDataGridCards.js";
export { renderLyoCell } from "./data-grid/LyoDataGridCell.js";
export { LyoDataGridExportMenu, LyoDataGridExportDialog } from "./data-grid/LyoDataGridExportMenu.js";
export { ExportColumnSelectorDialog } from "./data-grid/ExportColumnSelectorDialog.js";
export { buildConcreteQuery, buildProjectedQuery, buildRootQuery } from "./data-grid/buildQuery.js";
export { parameterHoverSummary } from "./data-grid/parameterSummary.js";
export { responseDevChips } from "./data-grid/devChips.js";
export type { LyoGridQueryMetrics } from "./data-grid/devChips.js";

export { LyoForm, useLyoForm, tryUseLyoForm } from "./form/LyoForm.js";
export type { PatchRequest, PropertyChange } from "./form/LyoForm.js";
export {
  LyoFormInput,
  LyoFormGrid,
  LyoNullableTextField,
  LyoValidationWrapper,
  LyoCheckSelect,
} from "./form/inputs.js";
export type { LyoFormInputProps, LyoFormSelectOption, LyoValidationResult, LyoValidationWrapperHandle, LyoValidationWrapperProps } from "./form/inputs.js";
export { autoFieldSize, fieldSizeColumns, resolveFieldSize } from "./form/fieldSize.js";
export type { LyoFieldColumns, LyoFieldSize, LyoFormInputKind } from "./form/fieldSize.js";
export { buildPatchRequest, flagsSelected, flagsToggle, formatFormValue, parseFormValue, propertyChange, valuesEqual } from "./form/patch.js";
export type { LyoFlagOption, LyoParsedFormValue } from "./form/patch.js";
export { LyoBulkPatchDialog } from "./overlay/LyoBulkPatchDialog.js";
export type { LyoBulkPatchDialogProps } from "./overlay/LyoBulkPatchDialog.js";
export { bulkPatchData, bulkPatchRoute, bulkPatchSummary, lyoTypeForProperty } from "./overlay/bulkPatch.js";
export { LyoFileUpload } from "./file-upload/LyoFileUpload.js";

export { LyoRichTextEditor, LYO_RTE_FONTS, LYO_RTE_SIZES, LYO_RTE_CODE_LANGUAGES } from "./editors/LyoRichTextEditor.js";
export type { LyoRichTextEditorHandle, LyoRichTextEditorProps, LyoRichTextOption } from "./editors/LyoRichTextEditor.js";
export { JsonEditor, JsonTreeView, JsonTreeSearch, useJsonTreeState, JSON_KIND_COLORS } from "./editors/JsonEditor.js";
export type { JsonEditorProps, JsonEditorViewMode, JsonTreeViewProps, JsonTreeState } from "./editors/JsonEditor.js";
export {
  ROOT_PATH_KEY,
  valueKind,
  pathKey,
  ancestorKeys,
  flattenVisible,
  expandAll,
  collapseAll,
  displayValue,
  containerSummary,
  searchMatches,
  stepMatch,
  getAt,
  setAt,
  removeAt,
  addChild,
  renameKey,
  copyText,
  parseEditValue,
  editText,
} from "./editors/jsonTree.js";
export type { JsonPath, JsonPathSegment, JsonValueKind, JsonTreeRow } from "./editors/jsonTree.js";
export { LyoTextDiffViewer } from "./editors/LyoTextDiffViewer.js";
export type { LyoTextDiffViewerProps } from "./editors/LyoTextDiffViewer.js";
export { diffText, diffStats, searchHits, highlightSegments, stepHit } from "./editors/diffOptions.js";
export type { DiffOptions, DiffPart, SearchHit, NumberedHighlightSegment } from "./editors/diffOptions.js";
export { DataTablePreview } from "./editors/DataTablePreview.js";
export type { DataTablePreviewModel, DataTablePreviewProps } from "./editors/DataTablePreview.js";
export { IdWorkbench, IdResultPanel } from "./identifiers/IdWorkbench.js";
export {
  NANO_DEFAULT_ALPHABET,
  NANO_DEFAULT_SIZE,
  autoIncrementIds,
  generateGuids,
  generateNanoIds,
  guidTimestampMs,
  resolveGuidNamespace,
} from "./identifiers/idOptions.js";
export type { IdEntry, GuidVersion, GuidNamespaceKind, AutoIncrementType } from "./identifiers/idOptions.js";
export {
  LYO_TYPE_CATEGORIES,
  LyoTypes,
  allLyoTypes,
  clrListFullName,
  isLyoCollection,
  lyoTypeFromName,
  lyoTypesByCategory,
  registerLyoType,
} from "./types/lyoTypeCatalog.js";
export type { LyoTypeCategory, LyoTypeEditorKind, LyoTypeInfo, LyoTypeRegistration } from "./types/lyoTypeCatalog.js";
export {
  CUSTOM_TYPE_TOKEN,
  canWrapInCollection,
  chipValidation,
  collectionShape,
  composeFullName,
  concreteFullNameDisplay,
  concreteFullNameFromInput,
  defaultJsonFor,
  displayName as lyoTypeDisplayName,
  elementFullName,
  hueForCategory,
  isValidUriText,
  isWideEditor,
  needsConcreteFullName,
  pickerGroups,
  shouldResetValue,
  templateTypeName,
  toListKind,
  typeChip,
  validateTypedJson,
  valuePreview,
} from "./types/lyoTypeUi.js";
export type { LyoListJsonKind, LyoTypeCollectionShape, LyoTypePickerGroup } from "./types/lyoTypeUi.js";
export { editorModeFor, invalidChips, jsonToScalarText, parseListJson, scalarTextToJson, serializeListJson } from "./types/lyoTypeValue.js";
export type { LyoScalarCommit, LyoValueEditorMode } from "./types/lyoTypeValue.js";
export { LyoTypeChip, LyoTypeSelect, LyoTypeValueInput, LyoValueEditorProvider, useLyoValueEditor } from "./types/LyoType.js";
export type { LyoTypeChipProps, LyoTypeSelectProps, LyoTypeValueInputProps, LyoValueEditorCatalog, LyoValueEditorProps } from "./types/LyoType.js";

export {
  catalogAreas,
  catalogIndexUrl,
  catalogPackageUrl,
  dependencyLabel,
  fetchCatalogIndex,
  fetchCatalogPackage,
  filterCatalogPackages,
  groupDependencies,
  parseCatalogIndex,
  parseCatalogPackage,
} from "./catalog/catalog.js";
export type {
  CatalogBenchmarkItem,
  CatalogBenchmarks,
  CatalogDependency,
  CatalogDependencyGroup,
  CatalogExample,
  CatalogFetch,
  CatalogIndex,
  CatalogIndexPackage,
  CatalogLink,
  CatalogPackageDoc,
  CatalogSection,
} from "./catalog/catalog.js";
export { useCatalogPackage } from "./catalog/useCatalogPackage.js";
export type { CatalogPackageState } from "./catalog/useCatalogPackage.js";
export { CatalogSectionList } from "./catalog/CatalogSectionList.js";
export type { CatalogSectionListProps } from "./catalog/CatalogSectionList.js";
export { PackageDoc } from "./catalog/PackageDoc.js";
export type { PackageDocProps } from "./catalog/PackageDoc.js";
export { LyoDependencyGraph } from "./catalog/LyoDependencyGraph.js";
export type { LyoDependencyGraphProps } from "./catalog/LyoDependencyGraph.js";
export { LyoPackageCatalog } from "./catalog/LyoPackageCatalog.js";
export type { LyoPackageCatalogProps } from "./catalog/LyoPackageCatalog.js";

export { moveDown, moveItem, moveUp, showSectionLabel } from "./reorder/reorder.js";
export { LyoReorderList } from "./reorder/LyoReorderList.js";
export type { LyoReorderListProps } from "./reorder/LyoReorderList.js";
export { clearCache } from "./cache/cache.js";
export { CacheGrid } from "./cache/CacheGrid.js";
export type { CacheGridProps } from "./cache/CacheGrid.js";
export { createTreeLoader, filterNodes } from "./file-tree/fileTree.js";
export type { FileTreeLoader, FileTreeNode } from "./file-tree/fileTree.js";
export { LyoFileTree } from "./file-tree/LyoFileTree.js";
export type { LyoFileTreeProps } from "./file-tree/LyoFileTree.js";
