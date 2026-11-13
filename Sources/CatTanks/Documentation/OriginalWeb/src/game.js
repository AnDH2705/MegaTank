export async function launch(THREE){const file=new URLSearchParams(location.search).has('classic')?'./classic-game.js':'./meta-game.js';const module=await import(file);return module.launch(THREE);}
