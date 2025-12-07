import OpenAI from 'openai';

/**
 * Client for OpenAI ChatGPT API integration
 */
export class LLMClient {
    private client: OpenAI;
    private model: string = 'gpt-5.1'; // GPT-5.1 (NOT chatgpt-5.1)

    constructor(apiKey: string) {
        this.client = new OpenAI({
            apiKey: apiKey,
            dangerouslyAllowBrowser: false // Server-side only for security
        });
    }

    /**
     * Send a message to ChatGPT with Nexus context
     * @param userMessage The user's question/request
     * @param nexusContext The DSL bytecode context from daemon
     * @param onChunk Callback for streaming responses
     */
    async chat(
        userMessage: string,
        nexusContext: string,
        onChunk?: (text: string) => void
    ): Promise<string> {
        const systemPrompt = this.buildSystemPrompt(nexusContext);

        const messages: OpenAI.Chat.ChatCompletionMessageParam[] = [
            { role: 'system', content: systemPrompt },
            { role: 'user', content: userMessage }
        ];

        if (onChunk) {
            // Streaming response
            return await this.streamChat(messages, onChunk);
        } else {
            // Non-streaming response
            const completion = await this.client.chat.completions.create({
                model: this.model,
                messages: messages,
                temperature: 0.7,
                max_completion_tokens: 4000
            });

            return completion.choices[0]?.message?.content || 'No response from LLM';
        }
    }

    /**
     * Stream chat response chunk by chunk
     */
    private async streamChat(
        messages: OpenAI.Chat.ChatCompletionMessageParam[],
        onChunk: (text: string) => void
    ): Promise<string> {
        const stream = await this.client.chat.completions.create({
            model: this.model,
            messages: messages,
            temperature: 0.7,
            max_completion_tokens: 4000,
            stream: true
        });

        let fullResponse = '';

        for await (const chunk of stream) {
            const content = chunk.choices[0]?.delta?.content || '';
            if (content) {
                fullResponse += content;
                onChunk(content);
            }
        }

        return fullResponse;
    }

    /**
     * Build system prompt with Nexus context
     */
    private buildSystemPrompt(nexusContext: string): string {
        return `You are Nexus AI, an expert software engineering assistant integrated with Visual Studio Code.

# Your Role
You help developers understand, refactor, and improve their codebase using structured context extracted from their project.

# Context Format
Below is the Nexus DSL v0 bytecode, which contains:
- **RULES**: Architectural rules and best practices for this codebase
- **NODES**: Code entities (functions, services, controllers, etc.) with metadata
- **DECISIONS**: Previous architectural decisions and their rationale

# Instructions
1. Analyze the provided context carefully
2. Answer the user's question based on the code structure shown
3. Suggest improvements that align with the architectural rules
4. Reference specific nodes by their NodeId (e.g., "FN:123") when discussing code
5. Be concise but thorough
6. If you suggest changes, provide code snippets

# Nexus Context (DSL v0)
\`\`\`
${nexusContext}
\`\`\`

# Guidelines
- Respect the architectural rules (RULES section)
- Consider dependencies between nodes when suggesting changes
- Prioritize maintainability and code quality
- Flag potential violations of the stated rules

Now, please assist the developer with their request.`;
    }

    /**
     * Change the model being used
     */
    setModel(model: string) {
        this.model = model;
    }

    /**
     * Get current model
     */
    getModel(): string {
        return this.model;
    }
}
