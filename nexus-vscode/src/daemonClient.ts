import axios, { AxiosInstance } from 'axios';

/**
 * Client for communicating with Nexus Daemon REST API
 */
export class DaemonClient {
    private client: AxiosInstance;
    private baseUrl: string;

    constructor(baseUrl: string = 'http://localhost:5050') {
        this.baseUrl = baseUrl;
        this.client = axios.create({
            baseURL: baseUrl,
            timeout: 30000,
            headers: {
                'Content-Type': 'application/json'
            }
        });
    }

    /**
     * Compile context from a solution/workspace path
     */
    async compileContext(
        taskType: string,
        solutionPath: string,
        targets: string[] = []
    ): Promise<CompileContextResponse> {
        const request: CompileContextRequest = {
            taskType,
            solutionId: '',
            solutionPath,
            targets
        };

        const response = await this.client.post<CompileContextResponse>(
            '/context/compile',
            request
        );

        return response.data;
    }

    /**
     * Fetch code snippet from a specific file
     */
    async fetchCode(
        path: string,
        startLine: number,
        endLine: number
    ): Promise<{ path: string; code: string }> {
        const response = await this.client.get('/code/fetch', {
            params: { path, startLine, endLine }
        });

        return response.data;
    }

    /**
     * Check if daemon is reachable
     */
    async ping(): Promise<boolean> {
        try {
            await this.client.get('/swagger/index.html', { timeout: 2000 });
            return true;
        } catch {
            return false;
        }
    }

    getBaseUrl(): string {
        return this.baseUrl;
    }
}

// Type definitions matching the C# API contracts
export interface CompileContextRequest {
    taskType: string;
    solutionId: string;
    solutionPath: string;
    targets: string[];
}

export interface CompileContextResponse {
    bytecode: string;
    summary: string;
    targets: string[];
}
