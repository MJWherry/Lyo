import { TrashIcon } from "./TrashIcon";

function UploadIcon() {
  return (
    <svg viewBox="0 0 24 24" width="15" height="15" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden>
      <path d="M12 16V5" />
      <path d="M8 9l4-4 4 4" />
      <path d="M5 19h14" />
    </svg>
  );
}

function PagesIcon() {
  return (
    <svg viewBox="0 0 24 24" width="15" height="15" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden>
      <rect x="3" y="3" width="7" height="7" rx="1" />
      <rect x="14" y="3" width="7" height="7" rx="1" />
      <rect x="3" y="14" width="7" height="7" rx="1" />
      <rect x="14" y="14" width="7" height="7" rx="1" />
    </svg>
  );
}

export function CoverTile({
  src,
  onUpload,
  onClear,
  onPickPage,
  size = "default",
  alt = "",
}: {
  src: string | null;
  onUpload: (file: File) => void;
  onClear: () => void;
  onPickPage?: () => void;
  size?: "default" | "mini";
  alt?: string;
}) {
  function takeFile(file: File | undefined | null) {
    if (file) onUpload(file);
  }

  const uploadBtn = (
    <label className="page-thumb__icon" title="Upload cover">
      <input
        type="file"
        accept="image/*"
        onChange={(e) => {
          takeFile(e.target.files?.[0]);
          e.target.value = "";
        }}
      />
      <UploadIcon />
    </label>
  );

  const selectBtn = onPickPage ? (
    <button type="button" className="page-thumb__icon" title="Use a chapter page" aria-label="Use a chapter page" onClick={onPickPage}>
      <PagesIcon />
    </button>
  ) : null;

  const deleteBtn = src ? (
    <button type="button" className="page-thumb__icon page-thumb__icon--danger" aria-label="Remove cover" onClick={onClear}>
      <TrashIcon />
    </button>
  ) : null;

  return (
    <div className={`page-thumb cover-picker${size === "mini" ? " cover-picker--mini" : ""}`}>
      <div className="page-thumb__frame">
        {src ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={src} alt={alt} draggable={false} />
        ) : (
          <div className="page-thumb__placeholder" />
        )}
        <div className={`page-thumb__actions${src ? " page-thumb__actions--filled" : " page-thumb__actions--empty"}`}>
          {uploadBtn}
          {selectBtn}
          {deleteBtn}
        </div>
      </div>
    </div>
  );
}
