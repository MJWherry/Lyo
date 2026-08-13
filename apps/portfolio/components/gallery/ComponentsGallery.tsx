"use client";

import { useRef, useState } from "react";
import {
  LyoAlert,
  LyoButton,
  LyoCard,
  LyoFileUpload,
  LyoForm,
  LyoFormInput,
  LyoRichTextEditor,
  JsonEditor,
  LyoTextDiffViewer,
  IdWorkbench,
  DataTablePreview,
  LyoStack,
  LyoTextField,
  type LyoRichTextEditorHandle,
} from "lyo-web-components";

export function ComponentsGallery() {
  const [name, setName] = useState("Ada");
  const [html, setHtml] = useState("<p>Hello Lyo</p>");
  const [json, setJson] = useState('{\n  "ok": true\n}');
  const rte = useRef<LyoRichTextEditorHandle>(null);

  return (
    <LyoStack spacing={3}>
      <LyoCard sx={{ p: 2 }}>
        <LyoStack spacing={1} direction="row">
          <LyoButton variant="contained">Primary</LyoButton>
          <LyoButton variant="outlined">Outlined</LyoButton>
          <LyoTextField size="small" label="Sample" defaultValue="text" />
        </LyoStack>
        <LyoAlert severity="info" sx={{ mt: 2 }}>
          Primitives wrap MUI and pick up Lyo CSS variables via LyoProvider.
        </LyoAlert>
      </LyoCard>

      <LyoForm model={{ name }} onSubmit={() => undefined}>
        <LyoFormInput propertyName="Name" label="Name" value={name} onChange={setName} originalValue="Ada" />
      </LyoForm>

      <LyoFileUpload maxFiles={3} onFiles={() => undefined} />

      <LyoRichTextEditor ref={rte} value={html} onChange={setHtml} />
      <JsonEditor value={json} onChange={setJson} />
      <LyoTextDiffViewer original={"line a\nline b"} modified={"line a\nline c"} />
      <IdWorkbench />
      <DataTablePreview
        table={{
          columns: ["Id", "Name"],
          rows: [
            ["1", "Ada"],
            ["2", "Grace"],
          ],
        }}
      />
    </LyoStack>
  );
}
