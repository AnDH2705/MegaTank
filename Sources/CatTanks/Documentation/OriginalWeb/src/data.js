export const CAT_NAMES = ['Mướp', 'Khói', 'Bông', 'Tam Thể', 'Mun', 'Xiêm', 'Vằn', 'Tux', 'Mochi', 'Mây'];
const make = (name, kind, rate, damage, color, description, extra = {}) => ({ name, kind, rate, damage, color, description, speed: 780, range: 1450, ...extra });
export const TURRETS = [
 make('Pháo xuyên giáp', 'shell', 1.6, 100, '#ffc34b', 'Đạn pháo nổ, bỏ qua một phần giáp.', { splash: 90, pierce: 1 }),
 make('Pháo nòng đôi', 'double', 1.3, 58, '#ffe68a', 'Hai viên bắn lệch nhịp, tập trung một mục tiêu.'),
 make('Chùm tên lửa', 'rocket', 3.1, 90, '#ff8350', 'Ba tên lửa bám mục tiêu; nổ lan.', { count: 3, splash: 100, speed: 480 }),
 make('Gatling cầu vồng', 'rapid', .15, 12, '#ffe49b', 'Đạn liên thanh; tốc độ cao, độ tản nhỏ.'),
 make('Pháo ion', 'laser', 2.6, 115, '#69efff', 'Chùm sáng xuyên hàng quái; phá giáp.', { pierce: 5 }),
 make('Hỏa long', 'flame', .3, 28, '#ff862e', 'Phun lửa hình nón; thiêu đốt liên tục.', { range: 670, burn: 4, splash: 80, speed: 540 }),
 make('Cối cầu vồng', 'mortar', 2.7, 160, '#ffce75', 'Đạn cầu vồng bay cao rồi nổ rộng.', { splash: 160, speed: 390 }),
 make('Mắt đại bàng', 'sniper', 2.6, 230, '#b3f5ff', 'Ngắm quái ít máu; đạn xuyên giáp chính xác.', { pierce: 3 }),
 make('Tesla liên hoàn', 'tesla', 2.1, 76, '#59f6ff', 'Sét giật tối đa bốn mục tiêu, gây choáng.', { chain: 4, stun: .5 }),
 make('Trạm chỉ huy', 'drone', 3.8, 70, '#ffde7b', 'Điều khiển hai drone bắn đạn dẫn đường.', { count: 2, speed: 550 })
].map((v,i)=>({...v,id:`turret-${i}`,asset:`turret-${i}`,family:'THÁP PHÁO',flip:true}));
export const HANDHELD = [
 make('Paw-47', 'rapid', .6, 20, '#ffd675', 'Loạt ba viên đạn từ mèo bộ binh.', { count: 3 }),
 make('Mini Gatling', 'rapid', .17, 9, '#ffe5a3', 'Bắn liên thanh, ưu tiên quái bay.'),
 make('Bazooka cá ngừ', 'rocket', 3.6, 150, '#ff975b', 'Tên lửa bám đuôi, nổ lan.', { splash: 110, speed: 450 }),
 make('Súng trường trinh sát', 'sniper', 2.4, 145, '#cbf6a2', 'Một phát đạn tốc độ cao, xuyên hai quái.', { pierce: 2 }),
 make('Shotgun đỏ', 'shotgun', 1.4, 24, '#ffad78', 'Chùm năm viên tỏa quạt ở cự ly gần.', { count: 5, range: 680 }),
 make('Súng plasma', 'plasma', 1.2, 75, '#61ebff', 'Quả cầu plasma nổ, xuyên giáp.', { splash: 75, speed: 510 }),
 make('Súng phun lửa', 'flame', .35, 18, '#ffa342', 'Ngọn lửa duy trì thiêu đốt.', { range: 570, burn: 4, speed: 520 }),
 make('Súng phóng lựu', 'mortar', 2.3, 110, '#ffde68', 'Lựu đạn cầu vồng, sát thương vùng.', { splash: 130, speed: 400 }),
 make('Nỏ thợ săn', 'arrow', 1.5, 95, '#f5deb2', 'Mũi tên xuyên hai mục tiêu, gây chảy máu.', { pierce: 2, burn: 2 }),
 make('Drone bỏ túi', 'drone', 1.6, 45, '#79ecff', 'Drone cá nhân bay lên và khóa quái bay.', { speed: 570 })
].map((v,i)=>({...v,id:`gun-${i}`,asset:`gun-${i}`,family:'VŨ KHÍ CÁ NHÂN',flip:false}));
export const PACKS = [
 make('Ba lô Gatling','rapid',.2,12,'#ffdc80','Xả đạn liên thanh từ bệ phụ.'),
 make('Hỏa tiễn bốn nòng','rocket',3.6,54,'#ff965d','Bốn quả đạn tỏa ra rồi bám mục tiêu.',{count:4,splash:75,speed:420}),
 make('Đại bác hỗ trợ','shell',2.5,125,'#ffd378','Đạn pháo hạng nặng, nổ lan.',{splash:100}),
 make('Cuộn Tesla','tesla',2.4,70,'#55eaff','Sét chuyền qua ba mục tiêu.',{chain:3,stun:.6}),
 make('Tên lửa song sinh','rocket',2.8,86,'#ffb281','Hai tên lửa dẫn đường.',{count:2,splash:90,speed:470}),
 make('Hỏa diệm','flame',.4,25,'#ff8c39','Phun lửa và đốt quái.',{burn:5,range:620,speed:490}),
 make('Băng cực quang','ice',.9,50,'#a3f5ff','Đạn băng làm chậm 65% trong ba giây.',{slow:3,speed:620}),
 make('Độc tố xanh','poison',1.6,40,'#b6fa58','Mây độc lan rộng, gây độc sáu giây.',{poison:6,splash:130,speed:450}),
 make('Cưa vòng','saw',2.1,105,'#e4e6ff','Lưỡi cưa quay, xuyên ba quái.',{pierce:3,speed:590}),
 make('Mũi khoan','drill',2.5,155,'#ffcd66','Mũi khoan xuyên giáp dày.',{pierce:2,speed:650}),
 make('Laser đỏ','laser',2.4,120,'#ff596c','Tia laser xuyên đội hình.',{pierce:5}),
 make('Hạt nhân tím','plasma',2.6,160,'#d48aff','Cầu năng lượng nổ lớn.',{splash:155,speed:350}),
 make('Vuốt thép','claw',1.4,110,'#ffa798','Ba lưỡi vuốt lao ra ở cự ly ngắn.',{count:3,range:510,speed:600}),
 make('Khiên năng lượng','shield',8,0,'#6af2ff','Phát xung khiên bảo vệ pháo đài hai giây.'),
 make('Sóng trọng lực','gravity',3,75,'#a2b8ff','Kéo quái lại quanh điểm va chạm, làm chậm.',{splash:190,slow:3,speed:420}),
 make('Súng cối chiến thuật','mortar',2.8,160,'#ffdc8b','Đạn rơi từ trên cao, nổ vùng rộng.',{splash:150,speed:360}),
 make('Thiện xạ cam','sniper',3,215,'#fff4c1','Xuyên giáp, ưu tiên quái tinh anh.',{pierce:3}),
 make('Bom đầu lâu','bomb',3.8,225,'#ffb57d','Bom bay chậm, vụ nổ diện rộng.',{splash:190,speed:320}),
 make('Mảnh tinh thạch','crystal',2,50,'#ffdd74','Năm mảnh tinh thạch tỏa quạt.',{count:5,pierce:1}),
 make('Trạm cứu thương','heal',7,240,'#99ffc0','Hồi máu pháo đài và tăng năng lượng.')
].map((v,i)=>({...v,id:`pack-${i}`,asset:`pack-${i}`,family:'VŨ KHÍ PHỤ',flip:true}));
export const WEAPONS = [...TURRETS,...HANDHELD,...PACKS];
const monster = (name,asset,size,hp,speed,damage,behavior,description,color,extra={}) => ({name,asset,size,hp,speed,damage,behavior,description,color,...extra});
export const MONSTER_BALANCE = {size:2,hp:1.5,damage:.8};
export const MONSTERS = [
 monster('Kiến tiên phong','bug-0',77,260,55,130,'swarm','Nhỏ nhất, chạy nhanh thành đàn.','#ff9d65'),
 monster('Sói bóng đêm','beast-0',132,600,58,260,'wolf','Tăng tốc khi đồng loại ở gần.','#bca6d0'),
 monster('Ong sát thủ','bug-4',94,340,61,180,'bee','Bay cao theo đường sin, lao xuống chích.','#ffdc6b',{flying:true}),
 monster('Bọ sừng đỏ','bug-1',155,1000,23,350,'armor','Vỏ cứng giảm 48% đạn thường.','#ff777d',{armor:.48}),
 monster('Lợn rừng cuồng nộ','beast-1',157,900,31,490,'charge','Báo hiệu trước khi lao nhanh về phía xe.','#e8ae76'),
 monster('Nhện giăng tơ','bug-2',140,630,29,160,'web','Phun tơ từ xa, làm chậm di chuyển xe.','#c79ae0',{range:760}),
 monster('Bọ ngựa đao','bug-3',136,650,42,360,'leap','Nhảy qua tiền tuyến, tấn công liên tiếp.','#c5f47c'),
 monster('Gấu cuồng chiến','beast-2',196,1900,20,520,'enrage','Càng ít máu càng chạy nhanh và đánh mạnh.','#f4a776'),
 monster('Ấu trùng đất','bug-5',111,660,33,250,'burrow','Chui xuống đất theo chu kỳ, né phần lớn sát thương.','#ffe0a7'),
 monster('Hổ kiếm','beast-4',171,1150,51,370,'tiger','Lướt ngắn, né đạn khi đang lao.','#ffca57'),
 monster('Bọ cạp độc','bug-6',145,1100,23,190,'venom','Phun nọc tầm xa, gây độc pháo đài.','#c298ed',{range:720}),
 monster('Khỉ băng','beast-3',216,2500,18,350,'frost','Ném băng từ xa, làm chậm tốc độ súng.','#8eeaff',{range:790}),
 monster('Bướm thôi miên','bug-7',122,700,39,150,'moth','Bay cao, rải bụi độc theo ba hướng.','#edb569',{flying:true,range:730}),
 monster('Rùa dung nham','beast-5',207,2800,12,450,'lava','Giáp dày, phản xung lửa khi bị đánh gần.','#ffaf47',{armor:.6}),
 monster('Tê giác bọc giáp','beast-6',230,3200,17,730,'rhino','Tích lực rồi húc cực mạnh, có giáp trước.','#c3a0ef',{armor:.38}),
 monster('Bọ tinh thạch','bug-8',168,1700,20,290,'crystal','Vỏ tinh thể giảm năng lượng, bắn mảnh pha lê.','#ffce69',{range:680}),
 monster('Đại bàng sư tử','beast-7',201,2100,37,360,'griffin','Bay lượn, bắn lông vũ theo loạt.','#ffd99b',{flying:true,range:800}),
 monster('Hươu cổ thụ','beast-9',225,2900,16,200,'healer','Phát hào quang hồi máu cho quái xung quanh.','#b5ed8e',{range:880}),
 monster('Bọ chúa giáp gai','bug-9',249,4200,14,620,'queen','Triệu hồi kiến; bắn đạn độc từng đợt.','#ed8e9c',{armor:.32,range:760}),
 monster('Bạo long hắc nguyệt','beast-8',330,60000,13,850,'boss','Ba pha: phun lửa, gọi quái, cuồng nộ dưới 35% máu.','#ff767d',{range:850,armor:.2,boss:true})
].map(m=>({...m,size:m.size*MONSTER_BALANCE.size,hp:Math.round((m.boss?m.hp:m.hp*2)*MONSTER_BALANCE.hp),damage:m.damage*MONSTER_BALANCE.damage}));
export const WAVES = [
 {name:'TIẾNG ĐỘNG TRONG SƯƠNG',types:[0,1,2],count:13,spacing:2.2},
 {name:'LỚP GIÁP TIÊN PHONG',types:[3,4,5,6],count:13,spacing:2.4},
 {name:'NANH VUỐT CUỒNG NỘ',types:[7,8,9,10],count:12,spacing:2.7},
 {name:'BĂNG GIÁ & DUNG NHAM',types:[11,12,13,14],count:10,spacing:3},
 {name:'BẦY ĐÀN DƯỚI TRĂNG',types:[15,16,17,18],count:10,spacing:3.4},
 {name:'BẠO LONG HẮC NGUYỆT',types:[19],count:1,spacing:1}
];
export const SKILLS = {
 rally:{name:'Tiếp viện',cost:80,cooldown:16,duration:10},
 heal:{name:'Sửa chữa',cost:120,cooldown:20},
 freeze:{name:'Bão băng',cost:150,cooldown:24,duration:6},
 drone:{name:'Drone',cost:100,cooldown:20,duration:14},
 barrage:{name:'Mưa tên lửa',cost:200,cooldown:28},
 shield:{name:'Lá chắn',cost:120,cooldown:22,duration:7}
};

// v3.7 meta catalogs; classic combat above remains unchanged.
export {BALANCE} from './data/balance.js';
import {CATS} from './data/cats.js';
export {CATS};

import {createStages} from './data/stages.js';
export const STAGES=createStages(MONSTERS);

import {MONSTER_BOOK} from './data/monster_book.js';
export {MONSTER_BOOK};

export {SKILL_DEFS} from './data/skill_defs.js';

export {DAILY_QUESTS,SHOP_STOCK} from './data/economy.js';

import {catalog} from './catalog.js';
export const CAT_BY_ID=catalog(CATS),MONSTER_BY_ID=catalog(MONSTERS,'asset'),MONSTER_BOOK_BY_ID=catalog(MONSTER_BOOK,'speciesId'),WEAPON_BY_ID=catalog(WEAPONS),TURRET_BY_ID=catalog(TURRETS);
