import OpenAI from 'openai';

import { ILobeAgentRuntimeErrorType } from '../error';
import { ChatStreamPayload } from './chat';

export interface AgentInitErrorPayload {
  error: object;
  errorType: string | number;
}

export interface ChatCompletionErrorPayload {
  [key: string]: any;
  endpoint?: string;
  error: object;
  errorType: ILobeAgentRuntimeErrorType;
  provider: ModelProvider;
}

export interface CreateChatCompletionOptions {
  chatModel: OpenAI;
  payload: ChatStreamPayload;
}

export enum ModelProvider {
  OpenAI = 'openai',
}

export type ModelProviderKey = Lowercase<keyof typeof ModelProvider>;
