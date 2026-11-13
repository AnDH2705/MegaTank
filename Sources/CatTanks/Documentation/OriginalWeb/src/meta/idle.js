export class IdleSystem {
  constructor(balance, getStageIndex) {
    this.balance = balance.idle;
    this.getStageIndex = getStageIndex; // () => S
    this.lastAccrualUnix = 0;
  }
  now() { return Math.floor(Date.now() / 1000); }
  effectiveElapsed(now = this.now()) {
    if (now < this.lastAccrualUnix) return 0;
    return Math.min(now - this.lastAccrualUnix, this.balance.capSeconds);
  }
  peek(now = this.now()) {
    const idle = this.balance;
    const cap = idle.capSeconds;
    const elapsed = this.effectiveElapsed(now);
    const S = this.getStageIndex();
    const minutes = elapsed / 60, hours = elapsed / 3600, frac = cap ? elapsed / cap : 0;
    const stage = Math.max(S, 1);
    let coins = Math.floor(idle.goldBasePerMin * Math.pow(idle.goldGrowth, stage - 1) * minutes);
    let spiritDust = Math.floor(idle.dustBasePerMin * Math.pow(idle.dustGrowth, stage - 1) * minutes);
    let catXp = Math.floor((idle.catXpBasePerMin || 0) * Math.pow(idle.catXpGrowth || 1, stage - 1) * minutes);
    let forgeStone = Math.floor((idle.forgeBasePerHour + idle.forgePerStageHour * S) * hours);
    let summonScroll = Math.floor((idle.scrollPer12hBase + Math.floor(S / idle.scrollStageDiv)) * frac);
    let monsterChow = Math.floor((idle.chowPerMin || 0.1) * minutes);
    let beastCage = Math.floor((idle.beastCagePer12hBase + Math.floor(S / (idle.beastCageStageDiv || 4))) * frac);
    if (S === 0) {
      const m = idle.unclearedMultiplier;
      coins = Math.floor(coins * m); spiritDust = Math.floor(spiritDust * m); catXp = Math.floor(catXp * m);
      forgeStone = Math.floor(forgeStone * m); summonScroll = Math.floor(summonScroll * m);
      monsterChow = Math.floor(monsterChow * m); beastCage = Math.floor(beastCage * m);
    }
    return { coins, spiritDust, catXp, forgeStone, summonScroll, monsterChow, beastCage, elapsedSeconds: elapsed, capped: elapsed >= cap && cap > 0, stageIndex: S };
  }
  claim(wallet, bag, now = this.now()) {
    const rewards = this.peek(now);
    if(rewards.elapsedSeconds < this.balance.minDisplaySeconds) throw new Error('Cần ít nhất 1 phút');
    wallet.add('coins', rewards.coins);
    wallet.add('spiritDust', rewards.spiritDust);
    wallet.add('catXp', rewards.catXp || 0);
    wallet.add('forgeStone', rewards.forgeStone);
    wallet.add('summonScroll', rewards.summonScroll);
    if (bag) {
      bag.monsterChow = (bag.monsterChow || 0) + rewards.monsterChow;
      bag.beastCage = (bag.beastCage || 0) + rewards.beastCage;
    }
    this.lastAccrualUnix = Math.max(now,this.lastAccrualUnix);
    return rewards;
  }
}
