// Catalogs stay live when content packs append stable ids at boot.
export function catalog(rows,key='id'){return {get:id=>rows.find(row=>row[key]===id),has:id=>rows.some(row=>row[key]===id),values:()=>[...rows]};}
export function resolveSprite(available,asset,fallback='cat-0'){return available.has(asset)?asset:available.has(fallback)?fallback:available.keys().next().value;}
