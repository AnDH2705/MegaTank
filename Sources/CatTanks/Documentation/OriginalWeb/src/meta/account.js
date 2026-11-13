export class Account {
  constructor(balance) {
    this.balance = balance.account;
    this.level = 1;
    this.xp = 0;
  }
  xpToNext() { return this.balance.xpBase + this.balance.xpPerLevel * this.level; } // 15 + 4*level
  unlockedFloorCount() {
    const { maxFloors, floorsPerLevels } = this.balance;
    return Math.min(maxFloors, 1 + Math.floor(this.level / floorsPerLevels));
  }
  addXp(amount) {
    let gained = 0;
    let left = amount;
    while (left > 0 && this.level < this.balance.maxLevel) {
      const need = this.xpToNext() - this.xp;
      if (left >= need) { left -= need; this.xp = 0; this.level++; gained++; }
      else { this.xp += left; left = 0; }
    }
    return { levelsGained: gained, newLevel: this.level };
  }
}
export function grantAccountXp(state,amount,balance){if(!Number.isSafeInteger(amount)||amount<0)throw Error('Invalid XP');const account=new Account(balance);Object.assign(account,state.account);const result=account.addXp(amount);state.account={level:account.level,xp:account.xp};return result;}
