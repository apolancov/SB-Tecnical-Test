export interface AreaRecord {
  readonly id: string;
  readonly name: string;
}

export interface RequestTypeRecord {
  readonly id: string;
  readonly name: string;
  readonly description: string;
}

export const EmptyAreas: ReadonlyArray<AreaRecord> = [];
export const EmptyRequestTypes: ReadonlyArray<RequestTypeRecord> = [];