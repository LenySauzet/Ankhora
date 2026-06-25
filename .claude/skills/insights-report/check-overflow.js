// Overflow / layout-cleanliness check for the insights report & slideshow.
// Run inside the page via Playwright's browser_evaluate (paste this function, or
// `page.evaluate(...)`). Every returned counter MUST be 0 — otherwise something
// spills out of the presentation or out of its box and the artifact is NOT clean.
//
// What it catches:
//   - slideOverflow   : any element escaping its .slide bounds (deck only)
//   - hOverflow/pastViewport : horizontal page overflow (report — vertical page)
//   - edgeTouchesBox  : a flow-diagram edge label (.edge) overlapping a box rect
//                       (edge labels must sit in the GAP between boxes)
//   - textOverflowBox : any SVG text spilling outside the box that contains it
//
// Verify at the projector resolution first: browser_resize to 1920x1080.
() => {
  const eps = 1.0;
  const de = document.documentElement;
  const slides = [...document.querySelectorAll('.slide')];
  const SKIP = ['defs','lineargradient','stop','marker','path','script','style','template'];

  // 1a) deck: nothing leaves its slide
  let slideOverflow = 0; const slideSamples = [];
  slides.forEach((s, i) => {
    const sr = s.getBoundingClientRect();
    s.querySelectorAll('*').forEach(el => {
      if (SKIP.includes(el.tagName.toLowerCase())) return;
      const r = el.getBoundingClientRect(); if (!r.width && !r.height) return;
      if (r.left < sr.left-eps || r.right > sr.right+eps || r.top < sr.top-eps || r.bottom > sr.bottom+eps) {
        slideOverflow++; if (slideSamples.length<10) slideSamples.push({slide:i+1, cls:el.getAttribute('class')||el.tagName.toLowerCase(), txt:(el.textContent||'').trim().slice(0,20)});
      }
    });
  });

  // 1b) report (no slides): no horizontal page overflow
  const hOverflow = de.scrollWidth - de.clientWidth;
  let pastViewport = 0; const vSamples = [];
  if (!slides.length) document.querySelectorAll('body *').forEach(el => {
    if (SKIP.includes(el.tagName.toLowerCase())) return;
    const r = el.getBoundingClientRect(); if (!r.width && !r.height) return;
    if (r.right > de.clientWidth+eps || r.left < -eps) { pastViewport++; if(vSamples.length<10) vSamples.push({cls:el.getAttribute('class')||el.tagName.toLowerCase(), txt:(el.textContent||'').trim().slice(0,18), right:+r.right.toFixed(0)}); }
  });

  // 2) flow-diagram SVGs: text fits its box; edge labels don't touch boxes
  let edgeTouchesBox = 0, textOverflowBox = 0; const eS=[], tS=[];
  document.querySelectorAll('svg.flow').forEach(svg => {
    const boxes = [...svg.querySelectorAll('rect')].map(r => r.getBoundingClientRect());
    svg.querySelectorAll('text').forEach(tx => {
      const tr = tx.getBoundingClientRect(); const cls = tx.getAttribute('class')||'';
      const cx=(tr.left+tr.right)/2, cy=(tr.top+tr.bottom)/2;
      if (cls.includes('edge')) {
        for (const b of boxes){ const ix=Math.min(tr.right,b.right)-Math.max(tr.left,b.left), iy=Math.min(tr.bottom,b.bottom)-Math.max(tr.top,b.top);
          if (ix>eps && iy>eps){ edgeTouchesBox++; if(eS.length<8)eS.push({txt:(tx.textContent||'').trim().slice(0,16), overlapX:+ix.toFixed(1)}); break; } }
      } else {
        const host = boxes.find(b => cx>=b.left&&cx<=b.right&&cy>=b.top&&cy<=b.bottom);
        if (host && (tr.left<host.left-eps || tr.right>host.right+eps || tr.top<host.top-eps || tr.bottom>host.bottom+eps)){
          textOverflowBox++; if(tS.length<8)tS.push({txt:(tx.textContent||'').trim().slice(0,18), spillR:+(tr.right-host.right).toFixed(1), spillB:+(tr.bottom-host.bottom).toFixed(1)}); }
      }
    });
  });

  return { ok: slideOverflow===0 && pastViewport===0 && hOverflow<=eps && edgeTouchesBox===0 && textOverflowBox===0,
           slideOverflow, slideSamples, hOverflow, pastViewport, vSamples, edgeTouchesBox, eS, textOverflowBox, tS };
}
