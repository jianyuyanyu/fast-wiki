import { Button } from 'antd';
import { memo, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Center, Flexbox } from 'react-layout-kit';

import { ModelProvider } from '@/libs/agent-runtime';
import { useChatStore } from '@/store/chat';
import { GlobalLLMProviderKey } from '@/types/settings';

import BedrockForm from './Bedrock';
import ProviderApiKeyForm from './ProviderApiKeyForm';
import ProviderAvatar from './ProviderAvatar';

interface APIKeyFormProps {
  id: string;
  provider?: string;
}

const APIKeyForm = memo<APIKeyFormProps>(({ id, provider }) => {
  const { t } = useTranslation('error')as any

  const [resend, deleteMessage] = useChatStore((s) => [s.regenerateMessage, s.deleteMessage]);

  const apiKeyPlaceholder = useMemo(() => {
    switch (provider) {
      default: {
        return '*********************************';
      }
    }
  }, [provider]);

  return (
    <Center gap={16} style={{ maxWidth: 300 }}> (
        <ProviderApiKeyForm
          apiKeyPlaceholder={apiKeyPlaceholder}
          avatar={<ProviderAvatar provider={provider as ModelProvider} />}
          provider={provider as GlobalLLMProviderKey}
          showEndpoint={provider === ModelProvider.OpenAI}
        />
      )
      <Flexbox gap={12} width={'100%'}>
        <Button
          block
          onClick={() => {
            resend(id);
            deleteMessage(id);
          }}
          style={{ marginTop: 8 }}
          type={'primary'}
        >
          {t('unlock.confirm')}
        </Button>
        <Button
          onClick={() => {
            deleteMessage(id);
          }}
        >
          {t('unlock.closeMessage')}
        </Button>
      </Flexbox>
    </Center>
  );
});

export default APIKeyForm;
