// Original cats, original IDs; meta stats are tunable and independent of classic combat.
export const CATS=[
 {id:'cat-0',name:'Mướp',specialty:'operator',role:'TANK',rarity:'ELITE',baseStats:{maxHp:420,atk:36,def:30,speed:84},growth:{maxHp:36,atk:2.8,def:2,speed:.4}},
 {id:'cat-1',name:'Khói',specialty:'infantry',role:'MAGE',rarity:'RARE',baseStats:{maxHp:260,atk:54,def:14,speed:102},growth:{maxHp:22,atk:4.2,def:1,speed:.5}},
 {id:'cat-2',name:'Bông',specialty:'infantry',role:'HEALER',rarity:'ELITE',baseStats:{maxHp:300,atk:40,def:18,speed:92},growth:{maxHp:28,atk:3.2,def:1.4,speed:.4}},
 {id:'cat-3',name:'Tam Thể',specialty:'operator',role:'WARRIOR',rarity:'RARE',baseStats:{maxHp:360,atk:46,def:24,speed:90},growth:{maxHp:30,atk:3.6,def:1.6,speed:.45}},
 {id:'cat-4',name:'Mun',specialty:'infantry',role:'ASSASSIN',rarity:'ELITE',baseStats:{maxHp:240,atk:62,def:12,speed:116},growth:{maxHp:20,atk:4.6,def:.9,speed:.6}},
 {id:'cat-5',name:'Xiêm',specialty:'infantry',role:'RANGER',rarity:'RARE',baseStats:{maxHp:280,atk:50,def:16,speed:108},growth:{maxHp:24,atk:3.8,def:1.1,speed:.55}},
 {id:'cat-6',name:'Vằn',specialty:'operator',role:'WARRIOR',rarity:'COMMON',baseStats:{maxHp:320,atk:38,def:20,speed:88},growth:{maxHp:26,atk:3,def:1.4,speed:.4}},
 {id:'cat-7',name:'Tux',specialty:'operator',role:'TANK',rarity:'COMMON',baseStats:{maxHp:400,atk:30,def:28,speed:78},growth:{maxHp:34,atk:2.4,def:1.8,speed:.3}},
 {id:'cat-8',name:'Mochi',specialty:'operator',role:'HEALER',rarity:'RARE',baseStats:{maxHp:330,atk:42,def:22,speed:90},growth:{maxHp:28,atk:3.2,def:1.5,speed:.4}},
 {id:'cat-9',name:'Mây',specialty:'infantry',role:'RANGER',rarity:'COMMON',baseStats:{maxHp:270,atk:44,def:15,speed:100},growth:{maxHp:23,atk:3.4,def:1,speed:.5}}
].map(cat=>({...cat,sprite:cat.id,classic:true}));
