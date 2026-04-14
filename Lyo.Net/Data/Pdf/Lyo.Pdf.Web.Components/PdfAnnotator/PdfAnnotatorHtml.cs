using System.Text;
using System.Text.Json;
using Lyo.Common.Records;

namespace Lyo.Pdf.Web.Components.PdfAnnotator;

/// <summary>Generates HTML for the PDF bounding box annotator. Use <see cref="GetForBlazor" /> when embedding in Blazor (postMessage).</summary>
public static class PdfAnnotatorHtml
{
    /// <summary>Returns HTML for embedding in an iframe. Uses postMessage to send annotations to the parent window.</summary>
    /// <param name="pdfBase64">Base64-encoded PDF content.</param>
    /// <returns>Full HTML document string.</returns>
    /// <remarks>PDF.js is loaded via pinned CDN URLs embedded in this HTML (<c>import()</c> inside the iframe module).</remarks>
    public static string GetForBlazor(string pdfBase64) => GetAnnotatorHtmlCore(pdfBase64, true, null, true);

    internal static string GetAnnotatorHtml(string pdfUrl, string submitUrl) => GetAnnotatorHtmlCore(pdfUrl, false, submitUrl, false);

    /// <summary>
    /// Read-only PDF viewer with region overlays (same coordinate model as the annotator). <paramref name="annotationsJsonArray" /> must be a JSON array of
    /// {id,page,left,right,top,bottom}.
    /// </summary>
    public static string GetReadOnlyViewerForBlazor(string pdfBase64, string annotationsJsonArray)
    {
        using var parsed = JsonDocument.Parse(annotationsJsonArray);
        var annLiteral = parsed.RootElement.GetRawText();
        return BuildReadOnlyViewerHtml(annLiteral, pdfBase64);
    }

    private static string PdfJsImportScriptCdn()
    {
        const string ver = "4.0.379";
        var main = JsonSerializer.Serialize($"https://cdn.jsdelivr.net/npm/pdfjs-dist@{ver}/build/pdf.min.mjs");
        var worker = JsonSerializer.Serialize($"https://cdn.jsdelivr.net/npm/pdfjs-dist@{ver}/build/pdf.worker.min.mjs");
        return $"const pdfjsLib=await import({main});pdfjsLib.GlobalWorkerOptions.workerSrc={worker};";
    }

    private static string GetAnnotatorHtmlCore(string pdfUrlOrBase64, bool usePostMessage, string? submitUrl, bool pdfIsRawBase64)
    {
        var pdfUrlJson = JsonSerializer.Serialize(pdfUrlOrBase64);
        var submitUrlJson = submitUrl != null ? JsonSerializer.Serialize(submitUrl) : "null";
        var submitBlock = usePostMessage
            ? "window.parent.postMessage({ type: 'pdf-annotations', annotations }, '*');"
            : $"const res = await fetch({submitUrlJson}, {{ method: 'POST', headers: {{ 'Content-Type': '{FileTypeInfo.Json.MimeType}' }}, body: JSON.stringify({{ annotations }}) }}); if (res.ok) {{ status.textContent = 'Saved!'; window.close(); }} else status.textContent = 'Save failed.';";

        var notifyFn = usePostMessage
            ? "function notifyProgress(){window.parent.postMessage({ type: 'pdf-annotation-progress', annotations: annotations.slice() }, '*');}"
            : "function notifyProgress(){}";

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>PDF Bounding Box Annotator</title><style>");
        sb.Append("*{box-sizing:border-box}body{margin:0;font-family:system-ui,sans-serif;background:#1a1a2e;color:#eee}");
        sb.Append("#toolbar{display:flex;align-items:center;gap:12px;padding:8px 16px;background:#16213e;border-bottom:1px solid #0f3460}");
        sb.Append("#toolbar button{padding:8px 16px;cursor:pointer;border-radius:6px;font-weight:500}");
        sb.Append("#btnDone{background:#e94560;color:white;border:none}#btnDone:hover{background:#ff6b6b}");
        sb.Append("#status{font-size:14px;color:#a0a0a0}");
        sb.Append("#container{overflow:auto;padding:16px;max-height:calc(100vh - 52px);outline:none}");
        sb.Append(".page-wrap{position:relative;display:inline-block;margin-bottom:16px}");
        sb.Append(".page-wrap canvas{display:block;box-shadow:0 2px 8px rgba(0,0,0,0.3)}");
        sb.Append(".overlay{position:absolute;top:0;left:0;pointer-events:none}.overlay.draw{pointer-events:auto;cursor:crosshair}");
        sb.Append(".box{position:absolute;border:2px solid #e94560;background:rgba(233,69,96,0.15);pointer-events:none}");
        sb.Append(".box.completed{pointer-events:auto}");
        sb.Append(
            ".box-label{position:absolute;top:-22px;left:0;font-size:12px;color:#e94560;font-weight:600;white-space:nowrap;opacity:0;transition:opacity .15s;background:rgba(26,26,46,0.95);padding:2px 6px;border-radius:4px}.box:hover .box-label{opacity:1}");

        sb.Append(
            ".resize-h{position:absolute;width:10px;height:10px;background:#fff;border:1px solid #e94560;z-index:2;box-sizing:border-box;display:none}.resize-h.nw{top:-5px;left:-5px;cursor:nwse-resize}.resize-h.ne{top:-5px;right:-5px;cursor:nesw-resize}.resize-h.sw{bottom:-5px;left:-5px;cursor:nesw-resize}.resize-h.se{bottom:-5px;right:-5px;cursor:nwse-resize}");

        sb.Append(".box.completed.box-selected .resize-h{display:block}");
        sb.Append(".box.completed{cursor:move;user-select:none}");
        sb.Append(".box.completed.box-selected{border-color:#f5f5f5;box-shadow:0 0 0 2px rgba(255,255,255,0.45)}");
        sb.Append("#idInput{padding:6px 10px;border-radius:4px;border:1px solid #0f3460;background:#1a1a2e;color:#eee;width:180px}");
        sb.Append("</style></head><body><div id=\"toolbar\"><span id=\"status\">Draw a box, then enter ID. Click Done when finished.</span>");
        sb.Append("<input type=\"text\" id=\"idInput\" placeholder=\"Box ID (e.g. invoice_date)\" />");
        sb.Append("<button id=\"btnDone\">Done</button></div><div id=\"container\"></div>");
        sb.Append("<script type=\"module\">try{");
        if (pdfIsRawBase64) {
            sb.Append("const pdfBase64=").Append(pdfUrlJson).Append(";");
            sb.Append(PdfJsImportScriptCdn());
            sb.Append(
                "const pdfBytes=Uint8Array.from(atob(pdfBase64),c=>c.charCodeAt(0));const pdf=await pdfjsLib.getDocument({data:pdfBytes}).promise;const numPages=pdf.numPages;let annotations=[];const viewports=[];let boxUid=0;let drag=null;let selectedBid=null;");
        }
        else {
            sb.Append("const pdfUrl=").Append(pdfUrlJson).Append(";");
            sb.Append(PdfJsImportScriptCdn());
            sb.Append(
                "const pdf=await pdfjsLib.getDocument(pdfUrl).promise;const numPages=pdf.numPages;let annotations=[];const viewports=[];let boxUid=0;let drag=null;let selectedBid=null;");
        }

        sb.Append(notifyFn);
        sb.Append(
            "function removeBoxByBid(bid){const el=document.querySelector('.box.completed[data-bid=\"'+bid+'\"]');if(!el)return;annotations=annotations.filter(a=>a.bid!==bid);el.remove();if(selectedBid===bid)selectedBid=null;document.querySelectorAll('.box-selected').forEach(x=>x.classList.remove('box-selected'));notifyProgress();}function selectBox(bid){selectedBid=bid;document.querySelectorAll('.box.completed').forEach(el=>{el.classList.toggle('box-selected',Number(el.dataset.bid)===bid);});try{window.focus();}catch(e){}var c=document.getElementById('container');if(c){c.tabIndex=-1;c.focus();}}function clamp(v,lo,hi){return Math.max(lo,Math.min(hi,v));}function syncRectToAnnotation(rect,page,bid){const ann=annotations.find(a=>a.bid===bid);if(!ann)return;const vp=viewports[page];const L=parseFloat(rect.style.left),T=parseFloat(rect.style.top),W=parseFloat(rect.style.width),H=parseFloat(rect.style.height);const tl=vp.convertToPdfPoint(L,T),br=vp.convertToPdfPoint(L+W,T+H);ann.left=Math.min(tl[0],br[0]);ann.right=Math.max(tl[0],br[0]);ann.top=Math.max(tl[1],br[1]);ann.bottom=Math.min(tl[1],br[1]);notifyProgress();}function onDragMove(e){if(!drag)return;const om=drag.overlay.getBoundingClientRect();let mx=e.clientX-om.left,my=e.clientY-om.top;mx=clamp(mx,0,drag.overlay.clientWidth);my=clamp(my,0,drag.overlay.clientHeight);if(drag.type==='move'){const w=parseFloat(drag.rect.style.width),h=parseFloat(drag.rect.style.height);let nl=drag.startLeft+(e.clientX-drag.startMx),nt=drag.startTop+(e.clientY-drag.startMy);nl=clamp(nl,0,drag.overlay.clientWidth-w);nt=clamp(nt,0,drag.overlay.clientHeight-h);drag.rect.style.left=nl+'px';drag.rect.style.top=nt+'px';}else if(drag.type==='resize'){const{left,top,w:sw,h:sh}=drag.startRect;const corner=drag.corner;const r=left+sw,b=top+sh;let nl=left,nt=top,nw=sw,nh=sh;if(corner==='se'){nw=Math.max(5,mx-left);nh=Math.max(5,my-top);}else if(corner==='nw'){nl=Math.min(mx,r-5);nt=Math.min(my,b-5);nw=r-nl;nh=b-nt;}else if(corner==='ne'){nt=Math.min(my,b-5);nw=Math.max(5,mx-left);nh=b-nt;}else if(corner==='sw'){nl=Math.min(mx,r-5);nw=r-nl;nh=Math.max(5,my-top);}drag.rect.style.left=nl+'px';drag.rect.style.top=nt+'px';drag.rect.style.width=nw+'px';drag.rect.style.height=nh+'px';}}function endDrag(){if(!drag)return;window.removeEventListener('mousemove',onDragMove);window.removeEventListener('mouseup',endDrag);syncRectToAnnotation(drag.rect,drag.page,drag.bid);drag=null;document.body.style.cursor='';}function startMove(rect,page,bid,e){const overlay=rect.parentElement;drag={type:'move',rect,page,bid,overlay,startMx:e.clientX,startMy:e.clientY,startLeft:parseFloat(rect.style.left),startTop:parseFloat(rect.style.top)};window.addEventListener('mousemove',onDragMove);window.addEventListener('mouseup',endDrag);document.body.style.cursor='grabbing';e.preventDefault();}function startResize(rect,page,bid,corner,e){selectBox(bid);const overlay=rect.parentElement;drag={type:'resize',rect,page,bid,overlay,corner,startRect:{left:parseFloat(rect.style.left),top:parseFloat(rect.style.top),w:parseFloat(rect.style.width),h:parseFloat(rect.style.height)}};window.addEventListener('mousemove',onDragMove);window.addEventListener('mouseup',endDrag);e.preventDefault();}function wireupBox(rect,page,bid){['nw','ne','sw','se'].forEach(corner=>{const h=document.createElement('div');h.className='resize-h '+corner;h.addEventListener('mousedown',e=>{e.stopPropagation();e.preventDefault();selectBox(bid);startResize(rect,page,bid,corner,e);});rect.appendChild(h);});rect.addEventListener('mousedown',e=>{if(e.target.closest('.resize-h'))return;e.stopPropagation();selectBox(bid);startMove(rect,page,bid,e);});}");

        sb.Append(
            "const container=document.getElementById('container');const idInput=document.getElementById('idInput');const status=document.getElementById('status');let pendingBox=null;function isDeleteKey(e){return e.key==='Delete'||e.key==='Backspace'||e.code==='Delete'||e.code==='Backspace';}window.addEventListener('message',e=>{if(e.data?.type!=='lyo-annotator-delete')return;if(selectedBid==null)return;removeBoxByBid(selectedBid);});window.addEventListener('keydown',e=>{if(e.target===idInput||idInput.contains(e.target))return;if(e.target&&(e.target.tagName==='INPUT'||e.target.tagName==='TEXTAREA'))return;if(!isDeleteKey(e))return;if(selectedBid==null)return;e.preventDefault();removeBoxByBid(selectedBid);});");

        sb.Append("function finishPendingBox(){if(!pendingBox)return;const id=(idInput.value||'').trim();if(!id)return;");
        sb.Append(
            "if(annotations.some(a=>String(a.id).toLowerCase()===id.toLowerCase())){status.textContent='Annotation ID \"'+id+'\" already exists. Use a unique ID.';try{window.parent.postMessage({type:'lyo-annotator-warning',message:'Annotation ID \"'+id+'\" already exists. Use a unique ID.'},'*');}catch{}idInput.focus();idInput.select();return;}");

        sb.Append("const{rect,page}=pendingBox;const vp=viewports[page];");
        sb.Append("const pdfTopLeft=vp.convertToPdfPoint(parseFloat(rect.dataset.vpLeft),parseFloat(rect.dataset.vpTop));");
        sb.Append("const pdfBottomRight=vp.convertToPdfPoint(parseFloat(rect.dataset.vpRight),parseFloat(rect.dataset.vpBottom));");
        sb.Append("const bid=++boxUid;");
        sb.Append(
            "annotations.push({id,page,left:Math.min(pdfTopLeft[0],pdfBottomRight[0]),right:Math.max(pdfTopLeft[0],pdfBottomRight[0]),top:Math.max(pdfTopLeft[1],pdfBottomRight[1]),bottom:Math.min(pdfTopLeft[1],pdfBottomRight[1]),bid});");

        sb.Append("notifyProgress();");
        sb.Append("rect.dataset.bid=''+bid;const label=document.createElement('div');label.className='box-label';label.textContent=id;rect.appendChild(label);");
        sb.Append("rect.classList.add('completed');rect.dataset.pending='';wireupBox(rect,page,bid);selectBox(bid);");
        sb.Append(
            "idInput.value='';idInput.style.display='none';pendingBox.overlay.classList.add('draw');status.textContent='Box \"'+id+'\" added. Drag to move; corners to resize when selected. Delete removes selection. Draw another or click Done.';pendingBox=null;idInput.onkeydown=(ev)=>{if(ev.key==='Enter')finishPendingBox();};}");

        sb.Append("for(let p=1;p<=numPages;p++){const page=await pdf.getPage(p);const scale=1.5;const viewport=page.getViewport({scale});viewports[p]=viewport;");
        sb.Append("const wrap=document.createElement('div');wrap.className='page-wrap';wrap.dataset.page=p;");
        sb.Append("const canvas=document.createElement('canvas');const ctx=canvas.getContext('2d');canvas.height=viewport.height;canvas.width=viewport.width;");
        sb.Append("const renderContext={canvasContext:ctx,viewport};await page.render(renderContext).promise;");
        sb.Append(
            "const overlay=document.createElement('div');overlay.className='overlay draw';overlay.style.width=viewport.width+'px';overlay.style.height=viewport.height+'px';");

        sb.Append("let startX,startY,currentRect;");
        sb.Append(
            "overlay.addEventListener('mousedown',(e)=>{if(e.target!==overlay)return;selectedBid=null;document.querySelectorAll('.box-selected').forEach(el=>el.classList.remove('box-selected'));startX=e.offsetX;startY=e.offsetY;");

        sb.Append(
            "currentRect=document.createElement('div');currentRect.className='box';currentRect.style.left=startX+'px';currentRect.style.top=startY+'px';currentRect.style.width='0';currentRect.style.height='0';overlay.appendChild(currentRect);});");

        sb.Append("overlay.addEventListener('mousemove',(e)=>{if(!currentRect)return;const x=Math.min(startX,e.offsetX);const y=Math.min(startY,e.offsetY);");
        sb.Append(
            "const w=Math.abs(e.offsetX-startX);const h=Math.abs(e.offsetY-startY);currentRect.style.left=x+'px';currentRect.style.top=y+'px';currentRect.style.width=w+'px';currentRect.style.height=h+'px';});");

        sb.Append(
            "overlay.addEventListener('mouseup',(e)=>{if(!currentRect||e.target!==overlay)return;const w=parseFloat(currentRect.style.width);const h=parseFloat(currentRect.style.height);");

        sb.Append("if(w<5||h<5){currentRect.remove();currentRect=null;return;}overlay.classList.remove('draw');");
        sb.Append(
            "currentRect.dataset.vpLeft=Math.min(startX,e.offsetX);currentRect.dataset.vpTop=Math.min(startY,e.offsetY);currentRect.dataset.vpRight=Math.max(startX,e.offsetX);currentRect.dataset.vpBottom=Math.max(startY,e.offsetY);");

        sb.Append(
            "currentRect.dataset.page=p;currentRect.dataset.pending='1';pendingBox={rect:currentRect,page:p,overlay};idInput.style.display='inline';idInput.focus();status.textContent='Enter ID for this box, then press Enter';currentRect=null;});");

        sb.Append("overlay.addEventListener('mouseleave',()=>{if(pendingBox)return;if(currentRect){currentRect.remove();currentRect=null;overlay.classList.add('draw');}});");
        sb.Append("wrap.appendChild(canvas);wrap.appendChild(overlay);container.appendChild(wrap);}");
        sb.Append("idInput.onkeydown=(ev)=>{if(ev.key==='Enter')finishPendingBox();};");
        sb.Append("async function submitAnnotations(){if(pendingBox)finishPendingBox();status.textContent='Saving...';");
        sb.Append(submitBlock);
        sb.Append("}document.getElementById('btnDone').addEventListener('click',submitAnnotations);");
        sb.Append(
            "}catch(e){const st=document.getElementById('status');if(st)st.textContent='PDF error: '+(e&&e.message?e.message:e);console.error(e);try{window.parent.postMessage({type:'lyo-annotator-error',message:String(e&&e.message?e.message:e)},'*');}catch{}}");

        sb.Append("</script></body></html>");
        return sb.ToString();
    }

    private static string BuildReadOnlyViewerHtml(string annotationsJsonLiteral, string pdfBase64)
    {
        var pdfBase64Json = JsonSerializer.Serialize(pdfBase64);
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>PDF template preview</title><style>");
        sb.Append("*{box-sizing:border-box}body{margin:0;font-family:system-ui,sans-serif;background:#1a1a2e;color:#eee}");
        sb.Append("#container{overflow:auto;padding:16px;max-height:100vh}");
        sb.Append(".page-wrap{position:relative;display:inline-block;margin-bottom:16px}");
        sb.Append(".page-wrap canvas{display:block;box-shadow:0 2px 8px rgba(0,0,0,0.3)}");
        sb.Append(".overlay{position:absolute;top:0;left:0;pointer-events:none}");
        sb.Append(".box{position:absolute;border:2px solid #4ade80;background:rgba(74,222,128,0.12);pointer-events:none}");
        sb.Append(
            ".box-label{position:absolute;top:-20px;left:0;font-size:11px;color:#4ade80;font-weight:600;white-space:nowrap;background:rgba(26,26,46,0.95);padding:1px 6px;border-radius:4px}");

        sb.Append("</style></head><body><div id=\"container\"></div>");
        sb.Append("<script type=\"module\">try{");
        sb.Append("const pdfBase64=").Append(pdfBase64Json).Append(";");
        sb.Append("const annotations=").Append(annotationsJsonLiteral).Append(";");
        sb.Append(PdfJsImportScriptCdn());
        sb.Append(
            "const pdfBytes=Uint8Array.from(atob(pdfBase64),c=>c.charCodeAt(0));const pdf=await pdfjsLib.getDocument({data:pdfBytes}).promise;const numPages=pdf.numPages;const viewports=[];");

        sb.Append("const container=document.getElementById('container');");
        sb.Append("for(let p=1;p<=numPages;p++){const page=await pdf.getPage(p);const scale=1.5;const viewport=page.getViewport({scale});viewports[p]=viewport;");
        sb.Append("const wrap=document.createElement('div');wrap.className='page-wrap';");
        sb.Append("const canvas=document.createElement('canvas');const ctx=canvas.getContext('2d');canvas.height=viewport.height;canvas.width=viewport.width;");
        sb.Append("await page.render({canvasContext:ctx,viewport}).promise;");
        sb.Append("const overlay=document.createElement('div');overlay.className='overlay';overlay.style.width=viewport.width+'px';overlay.style.height=viewport.height+'px';");
        sb.Append("for(const a of annotations){if(a.page!==p)continue;const vp=viewports[p];");
        sb.Append("const x1=vp.convertToViewportPoint(a.left,a.top)[0];const y1=vp.convertToViewportPoint(a.left,a.top)[1];");
        sb.Append("const x2=vp.convertToViewportPoint(a.right,a.bottom)[0];const y2=vp.convertToViewportPoint(a.right,a.bottom)[1];");
        sb.Append("const left=Math.min(x1,x2);const top=Math.min(y1,y2);const w=Math.abs(x2-x1);const h=Math.abs(y2-y1);");
        sb.Append("const box=document.createElement('div');box.className='box';box.style.left=left+'px';box.style.top=top+'px';box.style.width=w+'px';box.style.height=h+'px';");
        sb.Append("const lab=document.createElement('div');lab.className='box-label';lab.textContent=a.id||'';box.appendChild(lab);");
        sb.Append("overlay.appendChild(box);}");
        sb.Append("wrap.appendChild(canvas);wrap.appendChild(overlay);container.appendChild(wrap);}");
        sb.Append(
            "}catch(e){const c=document.getElementById('container');if(c)c.textContent='PDF error: '+(e&&e.message?e.message:e);console.error(e);try{window.parent.postMessage({type:'lyo-annotator-error',message:String(e&&e.message?e.message:e)},'*');}catch{}}");

        sb.Append("</script></body></html>");
        return sb.ToString();
    }
}