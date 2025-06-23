import * as z from 'zod';
import { Logger } from '../../utils/logger.js';
import { McpUnity } from '../../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

const toolName = 'add_to_addressable';
const toolDescription = 'Adds multiple assets to the Addressable system';
const paramsSchema = z.object({
  assets: z.record(z.string()).describe('Key/value object where key is address and value is asset path'),
  groupName: z.string().optional().describe('Optional name of the Addressable group to add the asset to. If not provided, will use the default group')
});

export function registerAddToAddressableTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${toolName}`);
      
  server.tool(
    toolName,
    toolDescription,
    paramsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${toolName}`, params);
        const result = await toolHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${toolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${toolName}`, error);
        throw error;
      }
    }
  );
}

async function toolHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  const { assets, groupName } = params;
        
  if (!assets || Object.keys(assets).length === 0) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      'Required parameter "assets" must be provided as a non-empty object with address:path pairs'
    );
  }

  const response = await mcpUnity.sendRequest({
    method: toolName,
    params: {
      assets,
      groupName
    }
  });
  
  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.errors,
    );
  }

  return {
    content: [{
      type: response.type || 'text',
      text: response.message,
    }]
  };
} 